using Content.Shared._DEN.Language.Prototypes;
using Content.Shared._DEN.Language.Systems;
using Content.Shared.Chat;

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
}

public enum ChatMessagePartType
{
    Dialog,
    Action
}

public sealed partial class ComplexChatMessage
{
    public string OriginalMessage;
    public List<(ChatMessagePartType, string)> Parts = [];
    public LanguagePrototype Language;

    public ComplexChatMessage(string message, LanguagePrototype language)
    {
        OriginalMessage = message;
        Parts.Add((ChatMessagePartType.Dialog, message));
        Language = language;
    }

    public ComplexChatMessage(string message, LanguagePrototype language, char delimiter)
    {
        OriginalMessage = message;
        Language = language;
        var seen = false;
        var seenAt = 0;
        var i = 0;
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

    public string WrapMessage()
    {
        return "";
    }
}
