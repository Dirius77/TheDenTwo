using Content.Shared.Inventory;

namespace Content.Shared.SubFloor;

public abstract partial class SharedTrayScannerSystem
{
    [Dependency] private InventorySystem _inventorySystem = default!;
    
    private void InitializeDen()
    {
        SubscribeLocalEvent<TrayScannerComponent, ComponentStartup>(OnTrayStartup);
        SubscribeLocalEvent<TrayScannerComponent, ComponentShutdown>(OnTrayShutdown);
        
    }

    private void OnTrayStartup(Entity<TrayScannerComponent> entity, ref ComponentStartup args)
    {
        // Are we equipped anywhere on a person
        if (!_inventorySystem.InSlotWithAnyFlags(entity.Owner, SlotFlags.All))
            return;

        var wearer = Transform(entity).ParentUid;
        OnEquip(wearer);
    }
    
    private void OnTrayShutdown(Entity<TrayScannerComponent> entity, ref ComponentShutdown args)
    {
        // Are we equipped anywhere on a person
        if (!_inventorySystem.InSlotWithAnyFlags(entity.Owner, SlotFlags.All))
            return;
        
        var wearer = Transform(entity).ParentUid;
        OnUnequip(wearer);
    }
}