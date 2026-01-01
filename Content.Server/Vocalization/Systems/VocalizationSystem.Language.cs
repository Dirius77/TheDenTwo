using Content.Shared._DEN.Language.Systems;

namespace Content.Server.Vocalization.Systems;

public sealed partial class VocalizationSystem : EntitySystem
{
    [Dependency] private readonly LanguageSystem _languageSystem = default!;
    [Dependency] private readonly ILogManager _log = default!;

    protected ISawmill _sawmill = default!;

    public void InitializeLanguages()
    {
        _sawmill = _log.GetSawmill("vocalization");
    }
}
