using Content.Shared.Actions;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Sealable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SealableControllerComponent : Component
{
    [DataField, AutoNetworkedField] public EntProtoId Action = "ActionToggleClothingSeals";

    [DataField, AutoNetworkedField] public EntityUid? ActionEntity;
    
    [DataField, AutoNetworkedField] public List<string> ControlledSlots = new();

    [DataField("requiredSlot"), AutoNetworkedField]
    public SlotFlags RequiredFlags = SlotFlags.BACK;

    // Which controlled slot was the last one we checked. Used to continue to the next slot after each DoAfters
    [DataField, AutoNetworkedField]
    public int CurrentSlot = 0;
    
    // If the controller is currently in the process of sealing/unsealing parts.
    [DataField, AutoNetworkedField]
    public bool IsToggling = false;
    
    // Whether the current activity is sealing of unsealing parts.
    [DataField, AutoNetworkedField]
    public bool Sealing = true;
}