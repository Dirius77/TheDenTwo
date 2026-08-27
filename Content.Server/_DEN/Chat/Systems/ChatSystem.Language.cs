using System.Linq;
using System.Text;
using Content.Server._DEN.Language.EntitySystems;
using Content.Server._DEN.Language.Events;
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
    [Dependency] private LanguageSystem _language = default!;
    [Dependency] private SharedMindSystem _mindSystem = default!;

    /// <inheritdoc />
    public override void SendEntityComplexSpeech(EntityUid source,
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

        if (!_actionBlocker.CanSpeak(source, (languageEnt, languageEnt.Comp), chatChannel) &&
            !ignoreActionBlocker)
            return;

        var language = ProtoMan.Index(languageEnt.Comp.Language);

        // Do language transformation, things like accents.
        var message = TransformComplexSpeech(source, originalMessage);

        // Transformation could cause there to be nothing left of the message, in which case, don't bother.
        if (message.Parts.Count == 0)
            return;

        // Do the logic to figure out the SpeechVerbPrototype for this language and message.
        // We need this even if a forced verb is passed because it also influences things like bolding shouted text.
        var speech = GetComplexSpeechVerb(source, message, language, chatChannel);

        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            // We don't have a forced name, so figure out what our name should be displayed as.
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
            // Check for a speech verb override
            if (nameEv.SpeechVerb != null && ProtoMan.Resolve(nameEv.SpeechVerb, out var proto))
                speech = proto;
        }

        name = FormattedMessage.EscapeText(name);
        var verb = verbOverride ?? Loc.GetString(_random.Pick(speech.SpeechVerbStrings));

        if (language.WrapperOverrides is { } wrapperOverrides &&
            wrapperOverrides.TryGetValue(chatChannel, out var wrapperOverride))
            wrapperProto = wrapperOverride;

        var wrapper = ProtoMan.Index(wrapperProto);

        // TODO: It's still weird that this is hardcoded, but you can expand it anyway so it's not the end of the world.
        // Find all of the recipients in our provided range and send the message to them.
        foreach (var (session, data) in GetRecipients(source,
                     chatChannel == ChatChannel.Whisper ? WhisperClearRange : VoiceRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;

            if (chatChannel == ChatChannel.Whisper && entRange != MessageRangeCheckResult.Full)
                continue;

            var visibleName = name;

            var entHideChat = entRange == MessageRangeCheckResult.HideChat;

            // Don't bother checking the event if the player doesn't have an entity.s
            if (session.AttachedEntity is { Valid: true } playerEntity)
            {
                // Hide whispers based on LOS and from ghosts.
                if (chatChannel == ChatChannel.Whisper &&
                    (!_interaction.InRangeUnobstructed(source, playerEntity, WhisperClearRange)
                    || data.Observer))
                    continue;

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

        // Handle constructing and formatting the message for the purpose of logging and replay.
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

        var ev = new EntitySpokeEvent(source, languageEnt, message, radioChannel, verb, chatChannel);
        RaiseLocalEvent(source, ev, true);

        var evt = new LanguageSpokenWithEvent(source, message, radioChannel, chatChannel);
        RaiseLocalEvent(languageEnt, evt);

        // The message wasn't sent by a player, so don't log it. Prevents radios and cameras from causing a player's
        // message to be logged many times.
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

    /// <summary>
    /// Sends a complex message to a specific listener, handling that listener's unique interpretation of the message.
    /// </summary>
    /// <param name="source">The original source of the message.</param>
    /// <param name="listener">The entity listening to the message.</param>
    /// <param name="speakingEnt">The entity actually speaking the message, may be different from source
    /// in the case of telephones/holopads.</param>
    /// <param name="originalMessage">The message as it was sent before any listener based transformations are
    /// applied to it.</param>
    /// <param name="wrapper">The wrapper to be used for formatting the message as it is displayed to a player.</param>
    /// <param name="channel">The ChatChannel this message is being spoken on.</param>
    /// <param name="name">The name to be used for the message.</param>
    /// <param name="verb">The verb to be used for the message.</param>
    /// <param name="bold">If the message should be bolded.</param>
    /// <param name="hideChat">If the message should be hidden from the chat UI (just a popup).</param>
    /// <param name="radioChannel">The radio channel name to be included in the message.</param>
    /// <param name="color">Force the color of the message (IE, radio messages).</param>
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

        var language = ProtoMan.Index(speakingEnt.Comp.Language);

        var understandEv = new AttemptUnderstandingEvent(source, language);
        RaiseLocalEvent(listener, understandEv);

        // Some languages can't be seen at all if the user doesn't understand them, like telepathy or hiveminds.
        if (understandEv.HideMessage)
            return;

        var message = originalMessage;

        var understanding = ProtoMan.Index(SharedLanguageSystem.MinimumFluency);

        if (understandEv is { Handled: true, Understanding: not null })
        {
            understanding = ProtoMan.Index(understandEv.Understanding.Value.Comp.Fluency);
        }

        // Pass the message off to the language system to allow for mangling it based on the specific language.
        // This can also do things such as hide the name or change the verb involved with speaking.
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

        // If the modification completely removed the message, just don't bother.
        if (message.Parts.Count == 0)
            return;

        // Handle this listener's preferences in regard to seeing language fonts.
        var hasMaxUnderstanding = understanding >= ProtoMan.Index(SharedLanguageSystem.MaximumFluency);
        var useLanguageFont = true;
        if (_mindSystem.TryGetMind(listener, out var mindId, out _) &&
            TryComp<LanguageFontSuppressionComponent>(mindId, out var suppression))
        {
                useLanguageFont = !(suppression.AllFonts || hasMaxUnderstanding);
        }
        var hideLanguage = !(language.DisplayInChat &&
                             ProtoMan.Index(language.UnderstandingForDisplay) <= understanding) ||
                           understandEv.HideLanguage;

        // Put the pieces of the modified message together with the wrapper and various variables and send it off to them.
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

        wrappedMessage = _chatManager.PrependFollowButtonIfAppropriate(
                wrappedMessage,
                source,
                listener.Comp.PlayerSession.Channel); // TODO: this method of doing follow in chat sucks, but the original is worse.

        _chatManager.ChatMessageToOne(channel,
            unwrappedMessage,
            wrappedMessage,
            source,
            hideChat,
            listener.Comp.PlayerSession.Channel);
    }

    /// <summary>
    /// Constructs an unwrapped and wrapped version of a Complex message given various properties about the message.
    /// </summary>
    /// <param name="message">The complex message to use for construction.</param>
    /// <param name="wrapper">The wrapper prototype to use for wrapping the message.</param>
    /// <param name="language">The language that the message is in.</param>
    /// <param name="bold">If the message should be bold.</param>
    /// <param name="hideLanguage">If the language name should be hidden from the message.</param>
    /// <param name="useLanguageFont">If the language font should be included in the wrapping.</param>
    /// <param name="name">The name of the speaker.</param>
    /// <param name="verb">The verb to use for speaking.</param>
    /// <param name="channel">The radio channel name being spoken on, if any.</param>
    /// <param name="color">The override color to use, if any.</param>
    /// <returns>(string, string) of (unwrapped message, wrapped message)</returns>
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
            // Build the language prefix if the language should be visible.
            langStr = Loc.GetString(wrapper.Language,
                ("language", language.LocalizedAbbreviation),
                ("color", language.FontColor));
        }

        // Build the beginning parts of the message, language, speaker name, and channel being spoken on.
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
            // Combine tags back into emotes and dialog so they can be formatted.
            List<(ChatPart, string)> mergedParts = [];
            var workingSet = message.Parts;
            Log.Debug("====== BEFORE ======");
            foreach (var (kind, part) in workingSet)
            {
                Log.Debug("Got " + kind + ": [" + part + "]");
            }
            var lastSeen = workingSet[0];
            for (int i = 1; i < workingSet.Count; i++)
            {
                var current = workingSet[i];
                // Matching Dialog or Emote.
                if ((lastSeen.Item1 is ChatPart.Dialog or ChatPart.DialogTag
                    && current.Item1 is ChatPart.Dialog or ChatPart.DialogTag)
                    || (lastSeen.Item1 is ChatPart.Emote or ChatPart.EmoteTag
                        && current.Item1 is ChatPart.Emote or ChatPart.EmoteTag))
                {
                    lastSeen.Item2 += current.Item2;
                }
                // This means that they are different.
                else
                {
                    if (lastSeen.Item1 == ChatPart.DialogTag)
                        lastSeen.Item1 = ChatPart.Dialog;
                    else if (lastSeen.Item1 == ChatPart.EmoteTag)
                        lastSeen.Item1 = ChatPart.Emote;
                    mergedParts.Add(lastSeen);
                    lastSeen = current;
                }
            }
            mergedParts.Add(lastSeen);
            // Loop over the parts of the complex speech.
            // Dialog gets a lot of special formatting where as emotes just get default action formatting.
            foreach (var (kind, part) in mergedParts)
            {
                if (kind == ChatPart.Dialog || kind == ChatPart.DialogTag)
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

        // Put the prefix, verb, and the built message together to produce the final result.
        var wrapResult = Loc.GetString(mainWrapper,
            ("space", message.NeedsSpacing ? " " : ""),
            ("verb", needsVerb ? "" : verb + ", "),
            ("prefix", prefix),
            ("message", wrappedBuilder.ToString()),
            ("color", color ?? language.FontColor));
        return (unwrappedBuilder.ToString(), wrapResult);
    }

    // I have no idea why 'author' is here, The API is from SentEntityEmote
    /// <summary>
    /// Send a pure emote that still handles the special '!' ''' and ',' cases. This skips most of the language system.
    /// </summary>
    /// <param name="source">The entity that is emoting.</param>
    /// <param name="action">The message string being emoted.</param>
    /// <param name="range">The range in which the emote should be visible.</param>
    /// <param name="nameOverride">The forced name to use, if any.</param>
    /// <param name="hideLog">Skip logging this emote.</param>
    /// <param name="checkEmote">If we should check for emote text in the action and cause emotes to occur.</param>
    /// <param name="ignoreActionBlocker">If we should ignore things that would normally prevent emoting.</param>
    /// <param name="author">The author of the emote.</param>
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
}
