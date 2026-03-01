using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Content.Server._DEN.Language.EntitySystems;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;


public sealed partial class ChatSystem
{
    [Dependency] private readonly LanguageSystem _language = default!;

    public static readonly WrapperSet SpeakWrapper = new()
    {
        DialogWrapper = "chat-language-entity-speak-wrap-dialog",
        EmoteWrapper = "chat-language-entity-speak-wrap-emote",
        LanguageWrapper = "chat-language-entity-speak-wrap-language",
        PrefixWrapper = "chat-language-entity-speak-wrap-prefix",
        MessageWrapper = "chat-language-entity-speak-wrap-message",
        SingularMessageWrapper = "chat-language-entity-speak-wrap-message-singular",
        BoldType = "chat-language-entity-speak-bold",
    };

    public static readonly WrapperSet WhisperWrapper = new()
    {
        DialogWrapper = "chat-language-entity-whisper-wrap-dialog",
        EmoteWrapper =  "chat-language-entity-whisper-wrap-emote",
        LanguageWrapper = "chat-language-entity-whisper-wrap-language",
        PrefixWrapper = "chat-language-entity-whisper-wrap-prefix",
        MessageWrapper = "chat-language-entity-whisper-wrap-message",
        SingularMessageWrapper = "chat-language-entity-whisper-wrap-message-singular",
        BoldType = "chat-language-entity-whisper-bold",
    };

    public void SendEntityComplexSpeech(EntityUid source,
        ComplexChatMessage originalMessage,
        WrapperSet wrappers,
        ChatTransmitRange range,
        RadioChannelPrototype? channel,
        string? nameOverride,
        bool whisper,
        bool hideLog = false,
        bool ignoreActionBlocker = false,
        string? verbOverride = null,
        Entity<LanguageComponent?>? languageOverride = null)
    {
        // Getting this first makes sure that if the language defaulted to something new it is set for CanSpeak
        var retrievedLanguage = languageOverride ?? _language.GetCurrentLanguageEntity(source)?.AsNullable();
        if (retrievedLanguage is null)
        {
            Log.Warning("Entity: " + Name(source) + " attempted to speak without a language.");
            return;
        }

        var languageEnt = retrievedLanguage.Value;
        if (!Resolve(languageEnt, ref languageEnt.Comp))
            return;

        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        var message = TransformComplexSpeech(source, originalMessage);

        if (message.Parts.Count == 0)
            return;

        var speech = GetComplexSpeechVerb(source, message);

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

        var language = _prototypeManager.Index(languageEnt.Comp.Language);

        string verb;
        if (verbOverride != null)
        {
            verb = verbOverride;
        }
        else
        {
            verb = Loc.GetString(_random.Pick(speech.SpeechVerbStrings));
            if (whisper)
                verb = "whispers";
        }

        foreach (var (session, data) in GetRecipients(source, whisper ? WhisperMuffledRange : VoiceRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;

            if (whisper && entRange != MessageRangeCheckResult.Full)
                continue;

            var visibleName = name;

            var entHideChat = entRange == MessageRangeCheckResult.HideChat;

            // Don't bother checking the event if the player doesn't have an entity.
            if (session.AttachedEntity is { Valid: true } playerEntity)
            {
                SendComplexMessageToEntity(source,
                    playerEntity,
                    languageEnt.AsNullable(),
                    message,
                    wrappers,
                    whisper ? ChatChannel.Whisper : ChatChannel.Local,
                    visibleName,
                    verb,
                    speech.Bold,
                    whisper,
                    entHideChat,
                    null,
                    null);
            }
        }

        var (unwrappedMessage, wrappedMessage) = BuildComplexMessage(message,
            wrappers,
            language,
            speech.Bold,
            !language.DisplayInChat,
            true,
            name,
            verb,
            null,
            null);

        _replay.RecordServerMessage(
            new ChatMessage(whisper ? ChatChannel.Whisper : ChatChannel.Local,
                unwrappedMessage,
                wrappedMessage,
                GetNetEntity(source),
                null,
                MessageRangeHideChatForReplay(range)));

        var ev = new EntitySpokeLanguageEvent(source, languageEnt.AsNullable(), message, channel, verb, whisper);
        RaiseLocalEvent(source, ev, true);

        if (!HasComp<ActorComponent>(source) || hideLog)
            return;

        // Build the original string to check if TransformComplexSpeech changed it.
        var (original, _) = BuildComplexMessage(originalMessage,
            wrappers,
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
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source} as {name} in {languageName}: {original}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source} in {languageName}: {original}.");
        }
        else
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {source} as {name} in {languageName}, original: {original}, transformed: {unwrappedMessage}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {source} in {languageName}, original: {original}, transformed: {unwrappedMessage}.");
        }
    }

    public void SendComplexMessageToEntity(EntityUid source,
        Entity<ActorComponent?> listener,
        Entity<LanguageComponent?> speakingEnt,
        ComplexChatMessage originalMessage,
        WrapperSet wrappers,
        ChatChannel channel,
        string name,
        string verb,
        bool bold,
        bool whisper,
        bool hideChat,
        string? radioChannel,
        Color? color)
    {
        if (!Resolve(listener, ref listener.Comp))
            return;

        if (!Resolve(speakingEnt, ref speakingEnt.Comp))
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
            whisper,
            out name);

        var useLanguageFont = HasComp<LanguageFontSuppressionComponent>(listener);
        var hideLanguage = !(language.DisplayInChat &&
                             _prototypeManager.Index(language.UnderstandingForDisplay) <= understanding) ||
                           understandEv.HideLanguage;

        var (unwrappedMessage, wrappedMessage) = BuildComplexMessage(message,
            wrappers,
            language,
            bold,
            hideLanguage,
            useLanguageFont,
            name,
            verb,
            radioChannel,
            color
        );

        _chatManager.ChatMessageToOne(channel, unwrappedMessage, wrappedMessage, source, hideChat, listener.Comp.PlayerSession.Channel);
    }

    // Returns the unwrapped message, as well as a wrapped version of the message based on the provided settings.
    public (string, string) BuildComplexMessage(ComplexChatMessage message,
        WrapperSet wrapper,
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
            langStr = Loc.GetString(wrapper.LanguageWrapper,
                ("language", language.LocalizedAbbreviation),
                ("color", language.FontColor));

        var prefix = Loc.GetString(wrapper.PrefixWrapper,
            ("language", langStr),
            ("spacing", message.NeedsSeparation ? "(" : ""),
            ("spacingClose", message.NeedsSeparation ? ")" : ""),
            ("entityName", name),
            ("channel", channel is null ? "" : $"\\[{channel}\\]"));
        var wrappedBuilder = new StringBuilder();
        var unwrappedBuilder = new StringBuilder();

        var boldType = Loc.GetString(wrapper.BoldType);

        var mainWrapper = wrapper.MessageWrapper;
        // Special casing to get the " not to be in the bubble in pure dialog.
        if (message.Parts is [{ Item1: ChatPart.Dialog }])
        {
            var (_, part) = message.Parts[0];
            unwrappedBuilder.Append(message.Delimiter + part + message.Delimiter);
            wrappedBuilder.Append(Loc.GetString(wrapper.DialogWrapper,
                ("fontType", useLanguageFont ? language.FontId : "Default"),
                ("fontColor", color ?? language.FontColor),
                ("fontSize", language.FontSize),
                ("style", bold ? $"[{boldType}]" : ""),
                ("styleClose", bold ? $"[/{boldType}]" : ""),
                ("message", part)));

            mainWrapper = wrapper.SingularMessageWrapper;
        }
        else
        {
            foreach (var (kind, part) in message.Parts)
            {
                if (kind == ChatPart.Dialog)
                {
                    unwrappedBuilder.Append(message.Delimiter + part + message.Delimiter);
                    wrappedBuilder.Append(message.Delimiter);
                    wrappedBuilder.Append(Loc.GetString(wrapper.DialogWrapper,
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
                    wrappedBuilder.Append(Loc.GetString(wrapper.EmoteWrapper,
                        ("message", part)));
                }
            }
        }

        var wrapResult = Loc.GetString(mainWrapper,
            ("space", message.NeedsSpacing ? " " : ""),
            ("verb", message.IsDetailed ? "" : verb + ", "),
            ("prefix", prefix),
            ("message", wrappedBuilder.ToString()),
            ("color", color is null ? "" : color));
        return (unwrappedBuilder.ToString(), wrapResult);
    }

    private ComplexChatMessage TransformComplexSpeech(EntityUid sender, ComplexChatMessage message)
    {
        var processedMessages = new List<(ChatPart, string)>();
        foreach (var (kind, part) in message.Parts)
        {
            if (kind == ChatPart.Dialog)
            {
                var ev = new TransformSpeechEvent(sender, part);
                RaiseLocalEvent(sender, ev, true);
                if (string.IsNullOrEmpty(ev.Message))
                    continue;
                processedMessages.Add((kind, ev.Message));
            }
            else
            {
                processedMessages.Add((kind, part));
            }
        }

        return new ComplexChatMessage(message, processedMessages.ToArray());
    }

    private ComplexChatMessage ConvertMessageToComplex(string message)
    {
        var isDetailed = false;
        var needsSpacing = true;
        var needsSeparation = false;
        if (message.StartsWith('!'))
        {
            isDetailed = true;
            message = message[1..].Trim();
            if (message.StartsWith('"'))
            {
                needsSeparation = true;
            }
            else if (message.StartsWith(',') || message.StartsWith('\''))
            {
                needsSpacing = false;
            }
        }

        return new ComplexChatMessage(message, "\"", isDetailed, needsSpacing, needsSeparation);
    }

    private ComplexChatMessage SanitizeComplexMessage(
        EntityUid source,
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

    public static string CoalesceComplexMessage(ComplexChatMessage msg)
    {
        var builder = new StringBuilder();
        foreach (var (kind, part) in msg.Parts)
        {
            if (kind == ChatPart.Dialog)
            {
                builder.Append(msg.Delimiter);
                builder.Append(part);
                builder.Append(msg.Delimiter);
            }
            else
            {
                builder.Append(part);
            }
        }

        return builder.ToString();
    }

    public struct WrapperSet()
    {
        public LocId DialogWrapper;
        public LocId EmoteWrapper;
        public LocId LanguageWrapper;
        public LocId PrefixWrapper;
        public LocId MessageWrapper;
        public LocId SingularMessageWrapper;
        public LocId BoldType;
    }
}
