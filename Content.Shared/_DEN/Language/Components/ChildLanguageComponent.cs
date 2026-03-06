using Content.Shared._DEN.Language.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Language.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedLanguageSystem))]
public sealed partial class ChildLanguageComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid ParentLanguage;
}
