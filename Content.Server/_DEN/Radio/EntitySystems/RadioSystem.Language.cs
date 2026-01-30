using Content.Shared._DEN.Language.Systems;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioSystem : EntitySystem
{
    [Dependency] private readonly LanguageSystem _languageSystem = default!;
    [Dependency] private readonly ILogManager _log = default!;

    protected ISawmill _sawmill = default!;

    partial void InitializeLanguages()
    {
        _sawmill = _log.GetSawmill("radio");
    }
}
