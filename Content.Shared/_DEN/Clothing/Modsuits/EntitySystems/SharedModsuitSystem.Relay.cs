using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Sealable.EntitySystems;
using Content.Shared.Armor;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public abstract partial class SharedModsuitSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<ModsuitPartComponent, ClothingSealedEvent>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, ClothingUnsealedEvent>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, InventoryRelayedEvent<CoefficientQueryEvent>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, InventoryRelayedEvent<DamageModifyEvent>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, ArmorExamineEvent>(RelayModsuitEvent);
    }
    
    private void RelayModsuitEvent<T>(Entity<ModsuitPartComponent> entity, ref T args)
    {
        EntityUid? controller;
        if (_attachedClothingQuery.TryComp(entity, out var attachedComp))
        {
            controller = attachedComp.AttachedUid;
        }
        else if (_modsuitControllerQuery.HasComp(entity))
        {
            // We ARE the controller.
            controller = entity.Owner;
        }
        else
        {
            // Not actually a part of a modsuit?
            return;
        }
        
        var evt = new ModsuitRelayedEvent<T>(args, entity);
        foreach (var module in _moduleSystem.GetContainedModules(controller.Value))
        {
            RaiseLocalEvent(module, evt);
        }
        
        args = evt.Args;
    }
}