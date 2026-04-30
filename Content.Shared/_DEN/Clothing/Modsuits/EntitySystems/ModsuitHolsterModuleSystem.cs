using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Modules.Components;
using Content.Shared._DEN.Modules.EntitySystems;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Robust.Shared.Containers;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public sealed partial class ModsuitHolsterModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedModsuitSystem _modsuitSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ModulePowerSystem _powerSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<ModsuitHolsterModuleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ModsuitHolsterModuleComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<ModsuitHolsterModuleComponent, ModuleRemovedEvent>(OnModuleRemoved);

        SubscribeLocalEvent<ModsuitHolsterModuleComponent, ModsuitRelayedEvent<ClothingGotEquippedEvent>>(
            OnClothingEquipRelay);
        SubscribeLocalEvent<ModsuitHolsterModuleComponent, ModsuitRelayedEvent<ClothingGotUnequippedEvent>>(OnClothingUnequipRelay);
    }

    private void OnComponentInit(Entity<ModsuitHolsterModuleComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Slot = _containerSystem.EnsureContainer<ContainerSlot>(ent, ent.Comp.SlotId);
    }
    
    private void OnComponentRemove(Entity<ModsuitHolsterModuleComponent> ent, ref ComponentRemove args)
    {
        _containerSystem.EmptyContainer(ent.Comp.Slot);
        _containerSystem.ShutdownContainer(ent.Comp.Slot);
    }
    
    private void OnModuleRemoved(Entity<ModsuitHolsterModuleComponent> ent, ref ModuleRemovedEvent args)
    {
        _containerSystem.EmptyContainer(ent.Comp.Slot);
    }
    
    private void OnClothingEquipRelay(Entity<ModsuitHolsterModuleComponent> ent, ref ModsuitRelayedEvent<ClothingGotEquippedEvent> args)
    {
        if (!_modsuitSystem.PartMatchesModule(ent.Owner, args.Owner))
            return;

        if (ent.Comp.Slot.ContainedEntity is not {} containedItem)
            return;

        if (TryComp<ModulePowerDrawComponent>(ent, out var powerDraw))
        {
            if (!_modsuitSystem.TryGetModuleController(ent.Owner, out var controller))
                return;

            if (!_powerSystem.TryUseCharge(controller.Value.Owner, powerDraw.UseCharge))
                return;
        }
        
        var wearer = args.Args.Wearer;
        _inventorySystem.TryUnequip(wearer, ent.Comp.InventorySlot);
        _containerSystem.RemoveEntity(ent, containedItem);
        _inventorySystem.TryEquip(wearer, containedItem, ent.Comp.InventorySlot, silent: false);
    }
    
    private void OnClothingUnequipRelay(Entity<ModsuitHolsterModuleComponent> ent, ref ModsuitRelayedEvent<ClothingGotUnequippedEvent> args)
    {
        if (!_modsuitSystem.PartMatchesModule(ent.Owner, args.Owner))
            return;

        if (TryComp<ModulePowerDrawComponent>(ent, out var powerDraw))
        {
            if (!_modsuitSystem.TryGetModuleController(ent.Owner, out var controller))
                return;

            if (!_powerSystem.TryUseCharge(controller.Value.Owner, powerDraw.UseCharge))
                return;
        }
        
        var wearer = args.Args.Wearer;
        if (!_inventorySystem.TryGetSlotEntity(wearer, ent.Comp.InventorySlot, out var wornItem))
            return;

        _containerSystem.Insert(wornItem.Value, ent.Comp.Slot);
    }
}