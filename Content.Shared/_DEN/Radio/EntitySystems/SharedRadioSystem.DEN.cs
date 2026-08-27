using Content.Shared._DEN.Language.EntitySystems;

namespace Content.Shared.Radio.EntitySystems;

public abstract partial class SharedRadioSystem
{
    [Dependency] private SharedLanguageSystem _language = null!;
}
