using System.Linq;
using Content.Shared._DEN.CCVar;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public abstract partial class SharedChatSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;

    public static readonly ProtoId<LanguageWrapperPrototype> SpeakWrapper = "SpeakWrapper";
    public static readonly ProtoId<LanguageWrapperPrototype> WhisperWrapper = "WhisperWrapper";
    public static readonly string[] ChatAllowedTags = ["bolditalic", "bold", "color", "italic", "mono"];

    /// <summary>
    /// Attempts to make an entity speak using complex speech (languages, mixed actions and dialog).
    /// </summary>
    /// <param name="source">The entity doing the speaking.</param>
    /// <param name="originalMessage">The message before any modifications are applied.</param>
    /// <param name="wrapperProto">The wrapper to use for formatting the message to users.</param>
    /// <param name="chatChannel">The chat channel to speak on.</param>
    /// <param name="range">The range to which the message will attempt to be transmitted. Keep in mind that language
    /// features and things like radios and cameras may cause it to be broadcast outside this range.</param>
    /// <param name="radioChannel">The radio channel to speak on, or null if no radio is being used.</param>
    /// <param name="nameOverride">The name to display for the speaker in place of their usual one.</param>
    /// <param name="hideLog">Whether to ignore logging this message.</param>
    /// <param name="ignoreActionBlocker">Whether this speech attempt ignores things that would usually prevent speaking.</param>
    /// <param name="verbOverride">The verb to use for this message, if one is needed, skips usual verb selection.</param>
    /// <param name="languageOverride">Forces the use of this specific language entity rather than selecting the one
    /// that the entity is currently configured to speak.</param>
    public virtual void SendEntityComplexSpeech(EntityUid source,
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
    }

    // TODO: Kill the other spot where this is getting called from and move this into WhisperMuffle (if we even keep using it)
    /// <summary>
    /// Runs default whisper obfuscation on the dialog parts of the provided message based on the passed float.
    /// </summary>
    /// <param name="message">The message to obfuscate</param>
    /// <param name="amount">The percentage of the message that should be obfuscated</param>
    /// <returns>A new message with the dialog portions obfuscated</returns>
    public ComplexChatMessage ObfuscateComplexChatMessage(ComplexChatMessage message, float amount)
    {
        var newParts = new List<(ChatPart, string)>();
        foreach (var (kind, text) in message.Parts)
        {
            if (kind == ChatPart.Dialog)
            {
                var newText = ObfuscateMessageReadability(text, amount);
                newParts.Add((kind, newText));
            }
            else
            {
                newParts.Add((kind, text));
            }
        }

        return new ComplexChatMessage(message, newParts);
    }

    /// <summary>
    /// Finds the correct verb for an instance of complex speech. This takes into account the difference between dialog and actions when
    /// searching for the correct verb prototype.
    /// </summary>
    /// <param name="source">The entity to find the verb for.</param>
    /// <param name="message">The message to use for finding the verb.</param>
    /// <param name="language">The language being spoken.</param>
    /// <param name="channel">Which chat channel is being spoken on.</param>
    /// <returns>The correct speech verb prototype to use.</returns>
    public SpeechVerbPrototype GetComplexSpeechVerb(EntityUid source, ComplexChatMessage message, LanguagePrototype language, ChatChannel channel)
    {
        var lastDialog = message.Parts.LastOrDefault(p => p.Item1 == ChatPart.Dialog).Item2 ?? "";

        SpeechVerbPrototype? current = null;
        Dictionary<LocId, ProtoId<SpeechVerbPrototype>>? currentSuffixVerbs = null;
        if (language.SpeechVerbs is { } speechVerbs)
        {
            if (speechVerbs.TryGetValue(channel, out var channelVerbs))
            {
                current = ProtoMan.Index(channelVerbs.DefaultVerb);
                currentSuffixVerbs = channelVerbs.SuffixSpeechVerbs;
            }
        }

        if (currentSuffixVerbs is not null)
        {
            foreach (var (str, id) in currentSuffixVerbs)
            {
                var proto = ProtoMan.Index(id);
                if (lastDialog.EndsWith(Loc.GetString(str)) && proto.Priority >= (current?.Priority ?? 0))
                {
                    current = proto;
                }
            }
        }

        // if no applicable suffix verb return the normal one used by the entity
        return current ?? GetSpeechVerb(source, lastDialog);
    }

    /// <summary>
    /// Converts a string into a ComplexChatMessage. This assumes the entire message is dialog, but does also handle
    /// the special cases of starting a message with '!' '"' ',' or ''' for special formatting.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public ComplexChatMessage ConvertMessageToComplex(string message)
    {
        var isDetailed = false;
        var needsSpacing = true;
        var needsSeparation = false;
        if (_cfg.GetCVar(DenCCVars.DetailedSpeechEnabled) && message.StartsWith('!'))
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
}

public enum ChatPart
{
    Dialog,
    Emote,
    DialogTag,
    EmoteTag,
}

public readonly record struct ComplexChatMessage()
{
    public readonly string OriginalMessage = string.Empty;
    public readonly IReadOnlyList<(ChatPart, string)> Parts = [];
    public readonly string Delimiter = string.Empty;
    public readonly bool IsDetailed;
    public readonly bool NeedsSpacing;
    public readonly bool NeedsSeparation;

    /// <summary>
    /// Builds a new ComplexChatMessage with the new chat parts, but the same settings and original message as before.
    /// This is for use in quickly building a new ComplexChatMessage after modifying it in some way, IE performing translation mangling.
    /// </summary>
    /// <param name="primary">The original ComplexChatMessage to copy settings from.</param>
    /// <param name="parts">The new parts of the message.</param>
    public ComplexChatMessage(ComplexChatMessage primary, IReadOnlyList<(ChatPart, string)> parts) : this()
    {
        OriginalMessage = primary.OriginalMessage;
        Delimiter = primary.Delimiter;
        IsDetailed = primary.IsDetailed;
        NeedsSpacing = primary.NeedsSpacing;
        NeedsSeparation = primary.NeedsSeparation;
        Parts = parts;
    }

    /// <summary>
    /// Builds a new ComplexChatMessage from scratch, this will attempt to parse the message into dialog, actions, and tags.
    /// </summary>
    /// <param name="message">The full message string.</param>
    /// <param name="delimiter">Which character differentiates dialog and actions.</param>
    /// <param name="isDetailed">If the message should be considered detailed at all. Parsing is skipped if not and the
    /// entire message is assumed to be dialog.</param>
    /// <param name="needsSpacing">If spacing is needed between the speaker name and the message when formatting</param>
    /// <param name="needsSeparation">Whether the speaker name should be formatted as its own separate chunk of the
    /// message, IE: "(Bob) He performs some action." vs "Alice performs some action."</param>
    /// <param name="escapeMarkup">Whether to run markup escaping on this message.</param>
    public ComplexChatMessage(string message, string delimiter, bool isDetailed, bool needsSpacing, bool needsSeparation, bool escapeMarkup = false) : this()
    {
        OriginalMessage = message;
        Delimiter = delimiter;
        IsDetailed = isDetailed;
        NeedsSpacing = needsSpacing;
        NeedsSeparation = needsSeparation;
        if (escapeMarkup)
            message = FormattedMessage.EscapeText(message);

        var parsedMsg = FormattedMessage.FromMarkupPermissive(message);
        List<(ChatPart, string)> parts = [];
        if (!isDetailed)
        {
            foreach (var hunk in parsedMsg.Nodes)
            {
                parts.Add((hunk.IsPlainText ? ChatPart.Dialog : ChatPart.DialogTag, hunk.ToString()));
            }

            Parts = parts;
            return;
        }

        var outside = true;
        foreach (var hunk in parsedMsg.Nodes)
        {
            if (!hunk.IsPlainText)
            {
                parts.Add((outside ? ChatPart.EmoteTag : ChatPart.DialogTag, hunk.ToString()));
                continue;
            }

            // Don't swap output between tags.
            var pieces = hunk.ToString().Split(Delimiter);
            if (pieces.Length == 1 && !string.IsNullOrEmpty(pieces[0]))
            {
                parts.Add((outside ? ChatPart.Emote : ChatPart.Dialog, pieces[0]));
                continue;
            }

            foreach (var msgChunk in pieces)
            {
                if (!string.IsNullOrEmpty(msgChunk))
                {
                    parts.Add((outside ? ChatPart.Emote : ChatPart.Dialog, msgChunk));
                    outside = !outside;
                }
            }
        }

        Parts = parts;
    }
}
