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
        ProtoId<LanguagePrototype>? languageOverride = null)
    {
        // Getting this first makes sure that if the language defaulted to something new it is set for CanSpeak
        var languageProto = languageOverride ?? _language.GetCurrentLanguage(source);

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

        if (languageProto == null)
        {
            Log.Warning("Entity: " + Name(source) + " attempted to speak without a language.");
            return;
        }

        var language = _prototypeManager.Index(languageProto.Value);

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
                    message,
                    language,
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
            language.DisplayInChat,
            true,
            whisper,
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

        var ev = new EntitySpokeLanguageEvent(source, message, language, channel, verb, whisper);
        RaiseLocalEvent(source, ev, true);

        if (!HasComp<ActorComponent>(source) || hideLog)
            return;

        // Build the original string to check if TransformComplexSpeech changed it.
        var (original, _) = BuildComplexMessage(originalMessage,
            wrappers,
            language,
            speech.Bold,
            language.DisplayInChat,
            true,
            whisper,
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
        ComplexChatMessage originalMessage,
        LanguagePrototype language,
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

        var understandEv = new AttemptUnderstandingEvent(source, language);
        RaiseLocalEvent(listener, understandEv);

        if (understandEv.HideMessage)
            return;

        var message = originalMessage;

        // TODO: Make this also handled by the events.
        if (whisper)
            name = _examineSystem.InRangeUnOccluded(source, listener, WhisperMuffledRange) ? name : "Someone";

        if (understandEv.Handled)
        {
            // Don't bother doing the processing if we have an override, since we'll use that anyway.
            if (understandEv.Understanding is not null && understandEv.MessageOverride is null)
                message = _language.ModifyMessageWithLanguage(understandEv.Understanding.Value,
                    source,
                    listener,
                    message,
                    language,
                    whisper);
        }

        var useLanguageFont = HasComp<LanguageFontSuppressionComponent>(listener);

        var hideLanguage = !language.DisplayInChat;
        if (understandEv.HideLanguage)
            hideLanguage = false;

        var (unwrappedMessage, wrappedMessage) = BuildComplexMessage(message,
            wrappers,
            language,
            bold,
            hideLanguage,
            useLanguageFont,
            whisper,
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
        bool whisper,
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
            ("spacing", message.NeedsSpacing ? "" : "("),
            ("spacingClose", message.NeedsSpacing ? "" : ")"),
            ("entityName", name),
            ("channel", channel is null ? "" : $"\\[{channel}\\]"));
        var wrappedBuilder = new StringBuilder();
        var unwrappedBuilder = new StringBuilder();

        var style = "";
        var styleClose = "";
        if (bold)
        {
            style = "[bold]";
            styleClose = "[/bold]";
        }

        if (whisper)
        {
            style = "[italic]";
            styleClose = "[/italic]";
        }

        foreach (var (kind, part) in message.Parts)
        {
            if (kind == ChatPart.Dialog)
            {
                unwrappedBuilder.Append(message.Delimiter + part + message.Delimiter);
                wrappedBuilder.Append(Loc.GetString(wrapper.DialogWrapper,
                    ("fontType", useLanguageFont ? language.FontId : "Default"),
                    ("fontColor", color ?? language.FontColor),
                    ("fontSize", language.FontSize),
                    ("style", style),
                    ("styleClose", styleClose),
                    ("message", message.Delimiter + part + message.Delimiter)));
            }
            else
            {
                unwrappedBuilder.Append(part);
                wrappedBuilder.Append(Loc.GetString(wrapper.EmoteWrapper,
                    ("message", part)));
            }
        }

        var wrapResult = Loc.GetString(wrapper.MessageWrapper,
            ("space", message.NeedsSpacing ? " " : ""),
            ("verb", message.IsDetailed ? "" : verb + ", "),
            ("prefix", prefix),
            ("whisper", whisper ? "[italic]" : ""),
            ("whisperClose", whisper ? "[/italic]" : ""),
            ("message", wrappedBuilder.ToString()),
            ("color", color is null ? "" : color));
        Log.Debug("Wrap result: " + wrapResult);
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
        if (message.StartsWith('!'))
        {
            isDetailed = true;
            message = message[1..];
            if (message.StartsWith('\'') || message.StartsWith(','))
            {
                needsSpacing = false;
                message = message[1..];
            }
            else if (message.StartsWith('"'))
            {
                needsSpacing = false;
            }
        }

        return new ComplexChatMessage(message, "\"", isDetailed, needsSpacing);
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

    private bool TryProcessRadioOnComplexMessage(
        EntityUid source,
        ComplexChatMessage message,
        [NotNullWhen(true)] out ComplexChatMessage? newMessage,
        out RadioChannelPrototype? channel)
    {
        newMessage = null;
        if (!TryProcessRadioMessage(source, message.Parts.First().Item2, out var modMessage, out channel))
            return false;

        List<(ChatPart, string)> newParts = new(message.Parts.Count);
        newParts.AddRange(message.Parts);
        var first = newParts[0];
        first.Item2 = modMessage;
        newParts[0] = first;

        newMessage = new ComplexChatMessage(message, newParts);

        return true;
    }

    private string CoalesceComplexMessage(ComplexChatMessage msg)
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
    }
}
