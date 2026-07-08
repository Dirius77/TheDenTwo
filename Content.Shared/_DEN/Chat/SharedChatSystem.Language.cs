using System.Linq;
using Content.Shared._DEN.CCVar;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Utility;
using Content.Shared.Speech;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public abstract partial class SharedChatSystem
{

    [Dependency] private IConfigurationManager _cfg = default!;

    public static readonly string[] ChatAllowedTags = ["bolditalic", "bold", "color", "italic", "mono"];

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
        var lastDialog = message.Parts.LastOrDefault(p => p.Item1 == ChatPart.Dialog).Item2;

        SpeechVerbPrototype? current = null;
        Dictionary<LocId, ProtoId<SpeechVerbPrototype>>? currentSuffixVerbs = null;
        if (language.SpeechVerbs is { } speechVerbs)
        {
            if (speechVerbs.TryGetValue(channel, out var channelVerbs))
            {
                current = _prototypeManager.Index(channelVerbs.DefaultVerb);
                currentSuffixVerbs = channelVerbs.SuffixSpeechVerbs;
            }
        }

        if (currentSuffixVerbs is not null)
        {
            foreach (var (str, id) in currentSuffixVerbs)
            {
                var proto = _prototypeManager.Index(id);
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

        var outside = false;
        foreach (var hunk in parsedMsg.Nodes)
        {
            if (!hunk.IsPlainText)
            {
                parts.Add((outside ? ChatPart.DialogTag : ChatPart.EmoteTag, hunk.ToString()));
                continue;
            }

            // Don't swap output between tags.
            var pieces = hunk.ToString().Split(Delimiter);
            if (pieces.Length == 1 && !string.IsNullOrEmpty(pieces[0]))
            {
                parts.Add((outside ? ChatPart.Dialog : ChatPart.Emote, pieces[0]));
                continue;
            }
            
            foreach (var msgChunk in pieces)
            {
                if (!string.IsNullOrEmpty(msgChunk))
                    parts.Add((outside ? ChatPart.Dialog : ChatPart.Emote, msgChunk));
                outside = !outside;
            }
        }

        Parts = parts;
    }
}
