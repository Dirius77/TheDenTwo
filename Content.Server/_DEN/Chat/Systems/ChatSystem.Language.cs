using System.Text;
using Content.Shared._DEN.Language.Prototypes;
using Content.Shared._DEN.Language.Systems;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem : SharedChatSystem
{

    [Dependency] private readonly LanguageSystem _languageSystem = default!;
    [Dependency] private readonly ILogManager _log = default!;

    protected ISawmill _sawmill = default!;

    partial void InitializeLanguages()
    {
        _sawmill = _log.GetSawmill("chat");
    }

    private void SanitizeComplexChatMessage(ComplexChatMessage msg,
        EntityUid source,
        out List<string> emoteStrs,
        bool capitalize,
        bool punctuate,
        bool capitalizeTheWordI)
    {
        emoteStrs = new List<string>();
        for (var i = 0; i < msg.Parts.Count; i++)
        {
            if (msg.Parts[i].Item1 != ChatMessagePartType.Dialog)
                continue;

            var part = msg.Parts[i];
            var sanitized = SanitizeInGameICMessage(source, part.Item2[1..^1], out var emoteStr, capitalize, punctuate, capitalizeTheWordI);
            part.Item2 = part.Item2[0] + sanitized + part.Item2[^1];
            if (emoteStr is not null)
                emoteStrs.Add(emoteStr);

            if (string.IsNullOrEmpty(sanitized))
                msg.Parts.RemoveAt(i);
            else
                msg.Parts[i] = part;
        }
    }

    private void TransformComplexChatSpeech(EntityUid source, ComplexChatMessage msg)
    {
        for (var i = 0; i < msg.Parts.Count; i++)
        {
            if (msg.Parts[i].Item1 != ChatMessagePartType.Dialog)
                continue;

            var part = msg.Parts[i];
            part.Item2 = part.Item2[0] + TransformSpeech(source, part.Item2[1..^1], msg.Language) + part.Item2[^1];
            if(string.IsNullOrEmpty(part.Item2))
                msg.Parts.RemoveAt(i);
            else
                msg.Parts[i] = part;
        }
    }

    private (string, string) ConstructMessage(ComplexChatMessage msg)
    {
        var unobfBuilder = new StringBuilder();
        var obfBuilder = new StringBuilder();
        foreach (var part in msg.Parts)
        {
            if (part.Item1 != ChatMessagePartType.Dialog)
            {
                unobfBuilder.Append(part.Item2);
                obfBuilder.Append(part.Item2);
            }
            else
            {
                unobfBuilder.Append(part.Item2);
                obfBuilder.Append(part.Item2[0] + _languageSystem.ObfuscateMessageWithLanguage(part.Item2[1..^1], msg.Language) + part.Item2[^1]);
            }
        }
        return (unobfBuilder.ToString(), obfBuilder.ToString());
    }

    // Returns the unobfuscated, and obfuscated, versions of the message simultaneously.
    private (string, string) WrapMessage(ComplexChatMessage msg, string entityName, SpeechVerbPrototype speech)
    {
        var unobfBuilder = new StringBuilder();
        var obfBuilder = new StringBuilder();
        if (msg.Language.ShowInChat)
        {
            var langPrefix = Loc.GetString("chat-language-identifier-wrap",
                ("color", msg.Language.FontColor),
                ("language", Loc.GetString(msg.Language.Abbreviation)),
                ("size", "11"));
            unobfBuilder.Append(langPrefix);
            obfBuilder.Append(langPrefix);
        }

        var namePrefix = "";
        if (msg.IsDetailed)
        {
            if (msg.Separation)
            {
                namePrefix = Loc.GetString("chat-language-entity-say-separation-prefix",
                    ("entityName", entityName));
            }
            else
            {
                namePrefix = Loc.GetString("chat-language-entity-say-free-prefix",
                    ("entityName", entityName),
                    ("space", msg.UseSpace ? " " : string.Empty));
            }
        }
        else
        {
            namePrefix = Loc.GetString("chat-language-entity-say-verb-prefix",
                ("entityName", entityName),
                ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))));
        }
        unobfBuilder.Append(namePrefix);
        obfBuilder.Append(namePrefix);

        foreach (var (partType, text) in msg.Parts)
        {
            switch (partType)
            {
                case ChatMessagePartType.Dialog:
                    // Slice the string so we don't obfuscate the quotes. Range is exclusive and index isn't so this looks weird.
                    var obfDialog = text[0] + _languageSystem.ObfuscateMessageWithLanguage(text[1..^1], msg.Language) + text[^1];
                    obfBuilder.Append(Loc.GetString("chat-language-speech-wrap",
                        ("message", obfDialog),
                        ("fontType", speech.FontId),
                        ("fontSize", speech.FontSize),
                        ("color", msg.Language.FontColor)));
                    unobfBuilder.Append(Loc.GetString("chat-language-speech-wrap",
                        ("message", text),
                        ("fontType", speech.FontId),
                        ("fontSize", speech.FontSize),
                        ("color", msg.Language.FontColor)));
                    break;
                case ChatMessagePartType.Action:
                    var actionText = Loc.GetString("chat-language-action-wrap",
                        ("message", text));
                    unobfBuilder.Append(actionText);
                    obfBuilder.Append(actionText);
                    break;
            }
        }

        return (unobfBuilder.ToString(), obfBuilder.ToString());
    }

    // Returns, in order: Unobfuscated, Obfuscated, Distance Garbled, Obfuscated Distance Garbled.
    private (string, string, string, string) ConstructWhisper(ComplexChatMessage msg)
    {
        var unobfBuilder = new StringBuilder();
        var obfBuilder = new StringBuilder();
        var unobfGarbleBuilder = new StringBuilder();
        var obfGarbleBuilder = new StringBuilder();
        foreach (var part in msg.Parts)
        {
            if (part.Item1 != ChatMessagePartType.Dialog)
            {
                unobfBuilder.Append(part.Item2);
                obfBuilder.Append(part.Item2);
            }
            else
            {
                unobfBuilder.Append(part.Item2);
                unobfGarbleBuilder.Append(part.Item2[0] + ObfuscateMessageReadability(part.Item2[1..^1], 0.2f) + part.Item2[^1]);
                var obfText = _languageSystem.ObfuscateMessageWithLanguage(part.Item2[1..^1], msg.Language);
                obfBuilder.Append(part.Item2[0] + obfText + part.Item2[^1]);
                obfGarbleBuilder.Append(part.Item2[0] + ObfuscateMessageReadability(obfText, 0.2f) + part.Item2[^1]);
            }
        }
        return (unobfBuilder.ToString(), obfBuilder.ToString(), unobfGarbleBuilder.ToString(), obfGarbleBuilder.ToString());
    }

    // Returns, in order: Unobfuscated, Obfuscated, Distance Garbled, Obfuscated Distance Garbled, Unknown Unobfuscated, Unknown Obfuscated.
    // I kind of hate this.
    private (string, string, string, string, string, string) WrapWhisper(ComplexChatMessage msg, string entityName, SpeechVerbPrototype speech)
    {
        var unobfBuilder = new StringBuilder();
        var obfBuilder = new StringBuilder();
        var unobfGarbleBuilder = new StringBuilder();
        var obfGarbleBuilder = new StringBuilder();
        var unobfUnknownBuilder = new StringBuilder();
        var obfUnknownBuilder = new StringBuilder();
        if (msg.Language.ShowInChat)
        {
            var langPrefix = Loc.GetString("chat-language-identifier-wrap",
                ("color", msg.Language.FontColor),
                ("language", Loc.GetString(msg.Language.Abbreviation)),
                ("size", "10"));
            unobfBuilder.Append(langPrefix);
            obfBuilder.Append(langPrefix);
            unobfGarbleBuilder.Append(langPrefix);
            obfGarbleBuilder.Append(langPrefix);
            unobfUnknownBuilder.Append(langPrefix);
            obfUnknownBuilder.Append(langPrefix);
        }

        var namePrefix = "";
        if (msg.IsDetailed)
        {
            if (msg.Separation)
            {
                namePrefix = Loc.GetString("chat-language-entity-whisper-separation-prefix",
                    ("entityName", entityName));
            }
            else
            {
                namePrefix = Loc.GetString("chat-language-entity-whisper-free-prefix",
                    ("entityName", entityName),
                    ("space", msg.UseSpace ? " " : string.Empty));
            }
        }
        else
        {
            namePrefix = Loc.GetString("chat-language-entity-whisper-verb-prefix",
                ("entityName", entityName));
        }
        unobfBuilder.Append(namePrefix);
        obfBuilder.Append(namePrefix);
        unobfGarbleBuilder.Append(namePrefix);
        obfGarbleBuilder.Append(namePrefix);

        if (msg.IsDetailed)
        {
            if (msg.Separation)
            {
                namePrefix = Loc.GetString("chat-language-entity-whisper-separation-prefix",
                    ("entityName", "Someone"));
            }
            else
            {
                namePrefix = Loc.GetString("chat-language-entity-whisper-free-prefix",
                    ("entityName", "Someone"),
                    ("space", msg.UseSpace ? " " : string.Empty));
            }
        }
        else
        {
            namePrefix = Loc.GetString("chat-language-entity-whisper-verb-prefix",
                ("entityName", "Someone"));
        }
        unobfUnknownBuilder.Append(namePrefix);
        obfUnknownBuilder.Append(namePrefix);

        foreach (var (partType, text) in msg.Parts)
        {
            switch (partType)
            {
                case ChatMessagePartType.Dialog:
                    // Slice the string so we don't obfuscate the quotes. Range is exclusive and index isn't so this looks weird.
                    var obfDialog = text[0] + _languageSystem.ObfuscateMessageWithLanguage(text[1..^1], msg.Language) + text[^1];
                    var obfWrap = Loc.GetString("chat-language-whisper-wrap",
                        ("message", obfDialog),
                        ("fontType", speech.FontId),
                        ("color", msg.Language.FontColor));
                    obfBuilder.Append(obfWrap);
                    var obfGarble = obfDialog[0] + ObfuscateMessageReadability(obfDialog[1..^1], 0.2f) + obfDialog[^1];
                    var obfGarbleWrap = Loc.GetString("chat-language-whisper-wrap",
                        ("message", obfGarble),
                        ("fontType", speech.FontId),
                        ("color", msg.Language.FontColor));
                    obfUnknownBuilder.Append(obfGarbleWrap);

                    var unobfWrap = Loc.GetString("chat-language-whisper-wrap",
                        ("message", text),
                        ("fontType", speech.FontId),
                        ("color", msg.Language.FontColor));
                    unobfBuilder.Append(unobfWrap);
                    var unobfGarble = text[0] + ObfuscateMessageReadability(text[1..^1], 0.2f) + text[^1];
                    var unobfGarbleWrap = Loc.GetString("chat-language-whisper-wrap",
                        ("message", unobfGarble),
                        ("fontType", speech.FontId),
                        ("color", msg.Language.FontColor));
                    unobfUnknownBuilder.Append(unobfGarbleWrap);
                    break;
                case ChatMessagePartType.Action:
                    var actionText = Loc.GetString("chat-language-action-whisper-wrap",
                        ("message", text));
                    unobfBuilder.Append(actionText);
                    obfBuilder.Append(actionText);
                    unobfGarbleBuilder.Append(actionText);
                    obfGarbleBuilder.Append(actionText);
                    unobfUnknownBuilder.Append(actionText);
                    obfUnknownBuilder.Append(actionText);
                    break;
            }
        }

        return (unobfBuilder.ToString(), obfBuilder.ToString(), unobfGarbleBuilder.ToString(), obfGarbleBuilder.ToString(), unobfUnknownBuilder.ToString(), obfUnknownBuilder.ToString());
    }

    public enum ChatMessagePartType
    {
        Dialog,
        Action,
    }

    public sealed partial class ComplexChatMessage
    {
        public string OriginalMessage;
        public List<(ChatMessagePartType, string)> Parts = [];
        public LanguagePrototype Language;
        public bool UseSpace = true;
        public bool Separation;
        public bool IsDetailed;

        public ComplexChatMessage(string message, LanguagePrototype language)
        {
            message = FormattedMessage.EscapeText(message);
            OriginalMessage = message;
            Parts.Add((ChatMessagePartType.Dialog, '"' + message + '"'));
            Language = language;
        }

        public ComplexChatMessage(string message, LanguagePrototype language, char delimiter)
        {
            message = FormattedMessage.EscapeText(message);
            Language = language;
            var seen = false;
            var seenAt = 0;
            var i = 0;
            IsDetailed = message[i] == '!';
            if (IsDetailed)
            {
                i++;
                Separation = message[i] == '"';
                if (Separation)
                    i++;
                UseSpace = !(message[i] == '\'' || message[i] == ',');
                if (UseSpace)
                    i++;
            }
            OriginalMessage = message[i..];

            while (i < message.Length)
            {
                if (message[i] == delimiter)
                {
                    if(seen)
                        i++;
                    Parts.Add((seen ? ChatMessagePartType.Dialog : ChatMessagePartType.Action, message[seenAt..i]));
                    seenAt = i;
                    seen = !seen;
                }

                i++;
            }

            if (seen)
            {
                var toAdd = message[seenAt..i];
                // We started a dialog, close it if it isn't.
                if(!toAdd.EndsWith(delimiter))
                    toAdd += delimiter;
                Parts.Add((ChatMessagePartType.Dialog, toAdd));
            }
            // We have text left at the end.
            else if (seenAt != message.Length)
            {
                Parts.Add((ChatMessagePartType.Action, message[seenAt..i]));
            }
        }
    }
}
