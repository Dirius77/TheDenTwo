using Content.Shared._DEN.Wires.Components;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.Wires;

public abstract partial class SharedWiresSystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    
    private void InitializeLock()
    {
        SubscribeLocalEvent<PanelLocksItemSlotComponent, PanelChangedEvent>(OnPanelChanged);
        SubscribeLocalEvent<PanelLocksItemSlotComponent, MapInitEvent>(OnMapInit);
    }

    private void OnPanelChanged(Entity<PanelLocksItemSlotComponent> entity, ref PanelChangedEvent evt)
    {
        foreach (var slot in entity.Comp.LockedSlots)
        {
            _itemSlots.SetLock(entity.Owner, slot, !evt.Open);
        }
    }

    // The event doesn't occur for the component loading, so make sure the lock is set correctly at MapInit.
    private void OnMapInit(Entity<PanelLocksItemSlotComponent> entity, ref MapInitEvent evt)
    {
        if (!TryComp<WiresPanelComponent>(entity, out var wiresPanel)) 
            return;
        
        foreach (var slot in entity.Comp.LockedSlots)
        {
            _itemSlots.SetLock(entity.Owner, slot, wiresPanel.Open);
        }
    }
}