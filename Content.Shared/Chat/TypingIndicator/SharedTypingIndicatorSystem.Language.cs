using Content.Shared._DEN.Language.Systems;

namespace Content.Shared.Chat.TypingIndicator;

public abstract partial class SharedTypingIndicatorSystem : EntitySystem
{
    [Dependency] private readonly LanguageSystem _languageSystem = default!;
    [Dependency] private readonly ILogManager _log = default!;

    protected ISawmill _sawmill = default!;

    partial void InitializeLanguages()
    {
        _sawmill = _log.GetSawmill("sharedtypingindicator");
    }
}
