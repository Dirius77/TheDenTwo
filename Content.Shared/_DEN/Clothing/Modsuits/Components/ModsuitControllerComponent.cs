using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModsuitControllerComponent : Component
{
    [DataField] public EntProtoId ActionId = "ActionOpenModsuitUI";
    
    [DataField] public EntityUid? UiActionEntity;

    [DataField, AutoNetworkedField] public bool PartsSpringlocked = false;
}