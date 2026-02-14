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

    private void SendEntityComplexSpeech(EntityUid source,
        ComplexChatMessage originalMessage,
        ChatTransmitRange range,
        RadioChannelPrototype? channel,
        string? nameOverride,
        bool whisper,
        bool hideLog = false,
        bool ignoreActionBlocker = false)
    {
        // Getting this first makes sure that if the language defaulted to something new it is set for CanSpeak
        var languageProto = _language.GetCurrentLanguage(source);

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

        var verb = Loc.GetString(_random.Pick(speech.SpeechVerbStrings));
        if (whisper)
            verb = "whispers";

        foreach (var (session, data) in GetRecipients(source, whisper ? WhisperMuffledRange : VoiceRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;

            if (whisper && entRange != MessageRangeCheckResult.Full)
                continue;

            string? msgOverride = null;
            var hideLanguage = !language.DisplayInChat;
            var understanding = _prototypeManager.Index(SharedLanguageSystem.MinimumFluency);
            var languageFont = true;

            var visibleName = name;

            // Don't bother checking the event if the player doesn't have an entity.
            if (session.AttachedEntity is { Valid: true } playerEntity)
            {
                var receiveEv = new AttemptUnderstandingEvent(source, languageProto.Value);
                RaiseLocalEvent(playerEntity, receiveEv);

                if (receiveEv.Cancelled)
                    continue;

                msgOverride = receiveEv.MessageOverride;

                if (whisper)
                    visibleName = _examineSystem.InRangeUnOccluded(source, playerEntity, WhisperMuffledRange) ? name : "Someone";

                if (receiveEv.HideLanguage)
                    hideLanguage = true;

                if (_language.UnderstandsLanguage(playerEntity,
                        languageProto.Value,
                        SharedLanguageSystem.MinimumFluency,
                        out var understandsEnt))
                    understanding = understandsEnt.Value.Comp.Fluency;

                if (HasComp<LanguageFontSuppressionComponent>(playerEntity) && understanding.Understanding > 0)
                    languageFont = false;
            }

            var entHideChat = entRange == MessageRangeCheckResult.HideChat;

            var (unwrappedSend, wrappedSend) = BuildComplexMessage(message,
                SpeakWrapper,
                language,
                understanding,
                speech.Bold,
                hideLanguage,
                languageFont,
                whisper,
                data.Observer ? 0 : data.Range, // I could just add another parameter but...
                visibleName,
                verb);

            _chatManager.ChatMessageToOne(whisper ? ChatChannel.Whisper : ChatChannel.Local, unwrappedSend, wrappedSend, source, entHideChat, session.Channel);
        }

        var (unwrappedMessage, wrappedMessage) = BuildComplexMessage(message,
            SpeakWrapper,
            language,
            _prototypeManager.Index(SharedLanguageSystem.MaximumFluency),
            speech.Bold,
            language.DisplayInChat,
            true,
            whisper,
            0,
            name,
            verb);

        _replay.RecordServerMessage(
            new ChatMessage(whisper ? ChatChannel.Whisper : ChatChannel.Local,
                unwrappedMessage,
                wrappedMessage,
                GetNetEntity(source),
                null,
                MessageRangeHideChatForReplay(range)));

        var ev = new EntitySpokeLanguageEvent(source, message, language, channel, whisper);
        RaiseLocalEvent(source, ev, true);

        if (!HasComp<ActorComponent>(source) || hideLog)
            return;

        var (original, _) = BuildComplexMessage(originalMessage,
            SpeakWrapper,
            language,
            _prototypeManager.Index(SharedLanguageSystem.MaximumFluency),
            speech.Bold,
            language.DisplayInChat,
            true,
            whisper,
            0,
            name,
            verb);

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

    // Returns the unwrapped message, as well as a wrapped version of the message based on the provided settings.
    private (string, string) BuildComplexMessage(ComplexChatMessage message,
        WrapperSet wrappers,
        LanguagePrototype language,
        LanguageFluencyPrototype understanding,
        bool bold,
        bool hideLanguage,
        bool useLanguageFont,
        bool whisper,
        float range,
        string name,
        string verb)
    {
        var langStr = "";
        if (!hideLanguage)
            langStr = Loc.GetString(wrappers.LanguageWrapper,
                ("language", Loc.GetString(language.Abbreviation)),
                ("color", language.FontColor));

        var prefix = Loc.GetString(wrappers.PrefixWrapper,
            ("language", langStr),
            ("spacing", message.NeedsSpacing ? "" : "("),
            ("spacingClose", message.NeedsSpacing ? "" : ")"),
            ("entityName", name));
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
                var understoodMsg = _language.ObfuscateMessageWithLanguage(part, language, understanding);
                understoodMsg = SanitizeMessagePeriod(understoodMsg);
                if (whisper && range > WhisperClearRange)
                    understoodMsg = ObfuscateMessageReadability(understoodMsg, 0.2f);
                unwrappedBuilder.Append(message.Delimiter + understoodMsg + message.Delimiter);
                wrappedBuilder.Append(Loc.GetString(wrappers.DialogWrapper,
                    ("fontType", useLanguageFont ? language.FontId : "Default"),
                    ("fontColor", language.FontColor),
                    ("fontSize", language.FontSize),
                    ("style", style),
                    ("styleClose", styleClose),
                    ("message", message.Delimiter + understoodMsg + message.Delimiter)));
            }
            else
            {
                unwrappedBuilder.Append(part);
                wrappedBuilder.Append(Loc.GetString(wrappers.EmoteWrapper,
                    ("message", part)));
            }
        }

        var wrapResult = Loc.GetString(wrappers.MessageWrapper,
            ("space", message.NeedsSpacing ? " " : ""),
            ("verb", message.IsDetailed ? "" : verb + ", "),
            ("prefix", prefix),
            ("whisper", whisper ? "[italic]" : ""),
            ("whisperClose", whisper ? "[/italic]" : ""),
            ("message", wrappedBuilder.ToString()));
        Log.Debug("Wrap result: " + wrapResult);
        return (unwrappedBuilder.ToString(), wrapResult);
    }

    private SpeechVerbPrototype GetComplexSpeechVerb(EntityUid source, ComplexChatMessage message)
    {
        // We won't even actually use this in this case.
        if (message.IsDetailed)
            return _prototypeManager.Index(DefaultSpeechVerb);

        var firstDialog = message.Parts.FirstOrDefault(p => p.Item1 == ChatPart.Dialog).Item2;

        return GetSpeechVerb(source, firstDialog);
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
