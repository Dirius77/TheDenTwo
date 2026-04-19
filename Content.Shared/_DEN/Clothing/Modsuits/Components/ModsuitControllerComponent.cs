using Content.Shared._DEN.Clothing.Modsuits.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedModsuitSystem))]
public sealed partial class ModsuitControllerComponent : Component
{
    [DataField] public EntProtoId ActionId = "ActionOpenModsuitUI";
    
    [DataField] public EntityUid? UiActionEntity;

    [DataField, AutoNetworkedField] public bool PartsSpringlocked = false;

    [DataField, AutoNetworkedField] public bool UIFunctional = true;

    [DataField, AutoNetworkedField] public Dictionary<string, EntityUid> SlotToPart = new();
    [DataField, AutoNetworkedField] public Dictionary<EntityUid, string> PartToSlot = new();
}