using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModsuitHolsterModuleComponent : Component
{
    /// <summary>
    /// The inventory slot that the holster will equip and unequip items from along with the attached part.
    /// </summary>
    [DataField, AutoNetworkedField] public string InventorySlot = "suitstorage";
    
    /// <summary>
    /// ID of the slot container for the holster.
    /// </summary>
    [DataField, AutoNetworkedField] public string SlotId = "MODHolster";

    /// <summary>
    /// The ContainerSlot that the holstered item will be stored in.
    /// </summary>
    [DataField] public ContainerSlot Slot = new();
}