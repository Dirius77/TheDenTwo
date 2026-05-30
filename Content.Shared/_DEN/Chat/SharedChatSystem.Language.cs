using System.Linq;
using Content.Shared._DEN.CCVar;
using Content.Shared._DEN.Language;
using Content.Shared.Speech;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public abstract partial class SharedChatSystem
{

    [Dependency] private IConfigurationManager _cfg = default!;

    // TODO: Kill the other spot where this is getting called from and move this into WhisperMuffle (if we even keep using it)
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
    Tag
}

public readonly record struct ComplexChatMessage()
{
    public readonly string OriginalMessage = string.Empty;
    public readonly IReadOnlyList<(ChatPart, string)> Parts = [];
    public readonly string Delimiter = string.Empty;
    public readonly bool IsDetailed;
    public readonly bool NeedsSpacing;
    public readonly bool NeedsSeparation;

    public ComplexChatMessage(ComplexChatMessage primary, IReadOnlyList<(ChatPart, string)> parts) : this()
    {
        OriginalMessage = primary.OriginalMessage;
        Delimiter = primary.Delimiter;
        IsDetailed = primary.IsDetailed;
        NeedsSpacing = primary.NeedsSpacing;
        NeedsSeparation = primary.NeedsSeparation;
        Parts = parts;
    }

    public ComplexChatMessage(string message, string delimiter, bool isDetailed, bool needsSpacing, bool needsSeparation, bool escapeMarkup = false) : this()
    {
        OriginalMessage = message;
        Delimiter = delimiter;
        IsDetailed = isDetailed;
        NeedsSpacing = needsSpacing;
        NeedsSeparation = needsSeparation;
        if (escapeMarkup)
            message = FormattedMessage.EscapeText(message);
        if (!isDetailed)
        {
            Parts = [(ChatPart.Dialog, message)];
            return;
        }

        var outside = false;
        List<(ChatPart, string)> parts = [];
        foreach (var msgChunk in message.Split(delimiter))
        {
            if (!string.IsNullOrEmpty(msgChunk))
                parts.Add((outside ? ChatPart.Dialog : ChatPart.Emote, msgChunk));
            outside = !outside;
        }

        Parts = parts;
    }
}
