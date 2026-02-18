using System.Linq;
using Content.Shared.Speech;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public abstract partial class SharedChatSystem
{
    public const char DetailedPrefix = '!';

    // TODO: Something better than this, especially if languages start controlling this.
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

    public SpeechVerbPrototype GetComplexSpeechVerb(EntityUid source, ComplexChatMessage message)
    {
        // We won't even actually use this in this case.
        if (message.IsDetailed)
            return _prototypeManager.Index(DefaultSpeechVerb);

        var firstDialog = message.Parts.FirstOrDefault(p => p.Item1 == ChatPart.Dialog).Item2;

        return GetSpeechVerb(source, firstDialog);
    }
}

public enum ChatPart
{
    Dialog,
    Emote,
}

public readonly record struct ComplexChatMessage()
{
    public readonly string OriginalMessage = string.Empty;
    public readonly IReadOnlyList<(ChatPart, string)> Parts = [];
    public readonly string Delimiter = string.Empty;
    public readonly bool IsDetailed;
    public readonly bool NeedsSpacing;

    public ComplexChatMessage(ComplexChatMessage primary, IReadOnlyList<(ChatPart, string)> parts) : this()
    {
        OriginalMessage = primary.OriginalMessage;
        Delimiter = primary.Delimiter;
        IsDetailed = primary.IsDetailed;
        NeedsSpacing = primary.NeedsSpacing;
        Parts = parts;
    }

    public ComplexChatMessage(string message, string delimiter, bool isDetailed, bool needsSpacing, bool escapeMarkup = true) : this()
    {
        OriginalMessage = message;
        Delimiter = delimiter;
        IsDetailed = isDetailed;
        NeedsSpacing = needsSpacing;
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
