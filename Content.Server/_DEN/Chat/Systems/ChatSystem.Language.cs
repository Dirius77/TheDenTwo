using System.Text;
using Content.Server._DEN.Language.EntitySystems;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Radio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public static readonly ProtoId<LanguageWrapperPrototype> SpeakWrapper = "SpeakWrapper";
    public static readonly ProtoId<LanguageWrapperPrototype> WhisperWrapper = "WhisperWrapper";

    public void SendEntityComplexSpeech(EntityUid source,
        ComplexChatMessage originalMessage,
        ProtoId<LanguageWrapperPrototype> wrapperProto,
        ChatChannel chatChannel,
        ChatTransmitRange range,
        RadioChannelPrototype? radioChannel = null,
        string? nameOverride = null,
        bool hideLog = false,
        bool ignoreActionBlocker = false,
        string? verbOverride = null,
        Entity<LanguageComponent>? languageOverride = null)
    {
        // Eat overrides so we can force the disabled language.
        if (!_language.LanguagesEnabled)
        {
            languageOverride = null;
        }

        // Getting this first makes sure that if the language defaulted to something new it is set for CanSpeak
        var retrievedLanguage = languageOverride ?? _language.GetCurrentLanguageEntity(source);
        if (retrievedLanguage is not { } languageEnt)
        {
            Log.Warning("Entity: " + Name(source) + " attempted to speak without a language.");
            return;
        }

        if (!_actionBlocker.CanSpeakLanguage(source, (languageEnt, languageEnt.Comp), chatChannel) &&
            !ignoreActionBlocker)
            return;

        var language = _prototypeManager.Index(languageEnt.Comp.Language);

        var message = TransformComplexSpeech(source, originalMessage);

        if (message.Parts.Count == 0)
            return;

        var speech = GetComplexSpeechVerb(source, message, language, chatChannel);

        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
            // Check for a speech verb override
            if (nameEv.SpeechVerb != null && _prototypeManager.Resolve(nameEv.SpeechVerb, out var proto))
                speech = proto;
        }

        name = FormattedMessage.EscapeText(name);

        var verb = verbOverride ?? Loc.GetString(_random.Pick(speech.SpeechVerbStrings));

        if (language.WrapperOverrides is { } wrapperOverrides &&
            wrapperOverrides.TryGetValue(chatChannel, out var wrapperOverride))
            wrapperProto = wrapperOverride;

        var wrapper = _prototypeManager.Index(wrapperProto);

        // TODO: It's still weird that this is hardcoded, but you can expand it anyway so it's not the end of the world.
        foreach (var (session, data) in GetRecipients(source,
                     chatChannel == ChatChannel.Whisper ? WhisperMuffledRange : VoiceRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;

            if (chatChannel == ChatChannel.Whisper && entRange != MessageRangeCheckResult.Full)
                continue;

            var visibleName = name;

            var entHideChat = entRange == MessageRangeCheckResult.HideChat;

            // Don't bother checking the event if the player doesn't have an entity.
            if (session.AttachedEntity is { Valid: true } playerEntity)
            {
                SendComplexMessageToEntity(source,
                    playerEntity,
                    languageEnt,
                    message,
                    wrapper,
                    chatChannel,
                    visibleName,
                    verb,
                    speech.Bold,
                    entHideChat,
                    null,
                    null);
            }
        }

        var (unwrappedMessage, wrappedMessage) = BuildComplexMessage(message,
            wrapper,
            language,
            speech.Bold,
            !language.DisplayInChat,
            true,
            name,
            verb,
            null,
            null);

        _replay.RecordServerMessage(new ChatMessage(chatChannel,
            unwrappedMessage,
            wrappedMessage,
            GetNetEntity(source),
            null,
            MessageRangeHideChatForReplay(range)));

        var ev = new EntitySpokeLanguageEvent(source, languageEnt, message, radioChannel, verb, chatChannel);
        RaiseLocalEvent(source, ev, true);

        if (!HasComp<ActorComponent>(source) || hideLog)
            return;

        // Build the original string to check if TransformComplexSpeech changed it.
        var (original, _) = BuildComplexMessage(originalMessage,
            wrapper,
            language,
            speech.Bold,
            !language.DisplayInChat,
            true,
            name,
            verb,
            null,
            null);

        var languageName = Loc.GetString(language.Name);

        if (original == unwrappedMessage)
        {
            if (name != Name(source))
            {
                _adminLogger.Add(LogType.Chat,
                    LogImpact.Low,
                    $"Say from {source} as {name} in {languageName}: {original}.");
            }
            else
            {
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source} in {languageName}: {original}.");
            }
        }
        else
        {
            if (name != Name(source))
            {
                _adminLogger.Add(LogType.Chat,
                    LogImpact.Low,
                    $"{chatChannel} from {source} as {name} in {languageName}, original: {original}, transformed: {unwrappedMessage}.");
            }
            else
            {
                _adminLogger.Add(LogType.Chat,
                    LogImpact.Low,
                    $"{chatChannel} from {source} in {languageName}, original: {original}, transformed: {unwrappedMessage}.");
            }
        }
    }

    public void SendComplexMessageToEntity(EntityUid source,
        Entity<ActorComponent?> listener,
        Entity<LanguageComponent> speakingEnt,
        ComplexChatMessage originalMessage,
        LanguageWrapperPrototype wrapper,
        ChatChannel channel,
        string name,
        string verb,
        bool bold,
        bool hideChat,
        string? radioChannel,
        Color? color)
    {
        if (!Resolve(listener, ref listener.Comp))
            return;

        var language = _prototypeManager.Index(speakingEnt.Comp.Language);

        var understandEv = new AttemptUnderstandingEvent(source, language);
        RaiseLocalEvent(listener, understandEv);

        if (understandEv.HideMessage)
            return;

        var message = originalMessage;

        var understanding = _prototypeManager.Index(SharedLanguageSystem.MinimumFluency);

        if (understandEv is { Handled: true, Understanding: not null })
        {
            understanding = _prototypeManager.Index(understandEv.Understanding.Value.Comp.Fluency);
        }

        message = _language.ModifyMessageWithLanguage(speakingEnt,
            source,
            listener,
            message,
            language,
            understanding,
            name,
            verb,
            channel,
            out name,
            out verb);

        if (message.Parts.Count == 0)
            return;

        var hasMaxUnderstanding = understanding >= _prototypeManager.Index(SharedLanguageSystem.MaximumFluency);
        var useLanguageFont = true;
        if (_mindSystem.TryGetMind(listener, out var mindId, out _) &&
            TryComp<LanguageFontSuppressionComponent>(mindId, out var suppression))
        {
                useLanguageFont = !(suppression.AllFonts || hasMaxUnderstanding);
        }
        var hideLanguage = !(language.DisplayInChat &&
                             _prototypeManager.Index(language.UnderstandingForDisplay) <= understanding) ||
                           understandEv.HideLanguage;

        var (unwrappedMessage, wrappedMessage) = BuildComplexMessage(message,
            wrapper,
            language,
            bold,
            hideLanguage,
            useLanguageFont,
            name,
            verb,
            radioChannel,
            color);

        _chatManager.ChatMessageToOne(channel,
            unwrappedMessage,
            wrappedMessage,
            source,
            hideChat,
            listener.Comp.PlayerSession.Channel);
    }

    // Returns the unwrapped message, as well as a wrapped version of the message based on the provided settings.
    public (string, string) BuildComplexMessage(ComplexChatMessage message,
        LanguageWrapperPrototype wrapper,
        LanguagePrototype language,
        bool bold,
        bool hideLanguage,
        bool useLanguageFont,
        string name,
        string verb,
        string? channel,
        Color? color)
    {
        var langStr = "";
        if (!hideLanguage)
        {
            langStr = Loc.GetString(wrapper.Language,
                ("language", language.LocalizedAbbreviation),
                ("color", language.FontColor));
        }

        var prefix = Loc.GetString(wrapper.Prefix,
            ("language", langStr),
            ("spacing", message.NeedsSeparation ? "(" : ""),
            ("spacingClose", message.NeedsSeparation ? ")" : ""),
            ("entityName", name),
            ("channel", channel is null ? "" : $"\\[{channel}\\]"));
        var wrappedBuilder = new StringBuilder();
        var unwrappedBuilder = new StringBuilder();

        var boldType = Loc.GetString(wrapper.BoldType);

        var mainWrapper = wrapper.Message;
        // Special casing to get the " not to be in the bubble in pure dialog.
        if (message.Parts is [{ Item1: ChatPart.Dialog }])
        {
            var (_, part) = message.Parts[0];
            unwrappedBuilder.Append(message.Delimiter + part + message.Delimiter);
            wrappedBuilder.Append(Loc.GetString(wrapper.Dialog,
                ("fontType", useLanguageFont ? language.FontId : "Default"),
                ("fontColor", color ?? language.FontColor),
                ("fontSize", language.FontSize),
                ("style", bold ? $"[{boldType}]" : ""),
                ("styleClose", bold ? $"[/{boldType}]" : ""),
                ("message", part)));

            mainWrapper = wrapper.SingularMessage;
        }
        else
        {
            foreach (var (kind, part) in message.Parts)
            {
                if (kind == ChatPart.Dialog)
                {
                    unwrappedBuilder.Append(message.Delimiter + part + message.Delimiter);
                    wrappedBuilder.Append(message.Delimiter);
                    wrappedBuilder.Append(Loc.GetString(wrapper.Dialog,
                        ("fontType", useLanguageFont ? language.FontId : "Default"),
                        ("fontColor", color ?? language.FontColor),
                        ("fontSize", language.FontSize),
                        ("style", bold ? $"[{boldType}]" : ""),
                        ("styleClose", bold ? $"[/{boldType}]" : ""),
                        ("message", part)));
                    wrappedBuilder.Append(message.Delimiter);
                }
                else
                {
                    unwrappedBuilder.Append(part);
                    wrappedBuilder.Append(Loc.GetString(wrapper.Emote, ("message", part)));
                }
            }
        }

        var needsVerb = message.IsDetailed || string.IsNullOrEmpty(verb);

        var wrapResult = Loc.GetString(mainWrapper,
            ("space", message.NeedsSpacing ? " " : ""),
            ("verb", needsVerb ? "" : verb + ", "),
            ("prefix", prefix),
            ("message", wrappedBuilder.ToString()),
            ("color", color ?? language.FontColor));
        return (unwrappedBuilder.ToString(), wrapResult);
    }

    private void SendEntityComplexEmote(EntityUid source,
        string action,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog = false,
        bool checkEmote = true,
        bool ignoreActionBlocker = false,
        NetUserId? author = null)
    {
        if (!_actionBlocker.CanEmote(source) && !ignoreActionBlocker)
            return;

        var isDetailed = action.StartsWith("!");
        if (isDetailed)
            action = action[1..];

        var useSpace = !(action.StartsWith("'") || action.StartsWith(",")) || isDetailed;

        var ent = Identity.Entity(source, EntityManager);
        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            // This may cause languages to interfere with a person's name even though they're emoting?
            // It's probably fine...
            // The other option is that pAIs don't inherit plushie names while emoting.
            // It DOES also make the voicemask work for changing your emote name.
            // Prebase has this.
            var nameEv = new TransformSpeakerNameEvent(source, Name(ent));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
        }
        name = FormattedMessage.EscapeText(name);

        var wrappedMessage = Loc.GetString("chat-language-entity-me-wrap-message",
            ("entity", ent),
            ("entityName", name),
            ("spacing", isDetailed ? "(" : ""),
            ("spacingClose", isDetailed ? ")" : ""),
            ("space", useSpace ? " " : ""),
            ("message", action));

        if (checkEmote &&
            !TryEmoteChatInput(source, action))
            return;

        SendInVoiceRange(ChatChannel.Emotes, action, wrappedMessage, source, range, author);
        if (!hideLog)
        {
            if (name != Name(source))
            {
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Emote from {source} as {name}: {action}");
            }
            else
            {
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Emote from {source}: {action}");
            }
        }
    }

    private ComplexChatMessage TransformComplexSpeech(EntityUid sender, ComplexChatMessage message)
    {
        var transformEvt = new TransformLanguageEvent(sender, message);
        RaiseLocalEvent(sender, transformEvt, true);

        return transformEvt.Message;
    }

    private ComplexChatMessage SanitizeComplexMessage(EntityUid source,
        ComplexChatMessage message,
        out List<string> emoteStrs,
        bool shouldCapitalize = true,
        bool punctuate = false,
        bool capitalizeTheWordI = true)
    {
        emoteStrs = [];
        var newParts = new List<(ChatPart, string)>(message.Parts.Count);
        foreach (var part in message.Parts)
        {
            if (part.Item1 == ChatPart.Dialog)
            {
                var sanitized = SanitizeInGameICMessage(source,
                    part.Item2,
                    out var emote,
                    shouldCapitalize,
                    punctuate,
                    capitalizeTheWordI);
                if (!string.IsNullOrEmpty(sanitized))
                    newParts.Add((part.Item1, sanitized));
                if (emote is not null)
                    emoteStrs.Add(emote);
            }
            else
            {
                newParts.Add((part.Item1, part.Item2));
            }
        }

        return new ComplexChatMessage(message, newParts);
    }
}
