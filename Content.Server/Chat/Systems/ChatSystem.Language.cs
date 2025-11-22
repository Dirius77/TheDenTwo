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
