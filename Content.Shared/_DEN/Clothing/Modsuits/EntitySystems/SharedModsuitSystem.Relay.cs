using System.Linq;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Sealable.EntitySystems;
using Content.Shared._DEN.Wires.Components;
using Content.Shared.Armor;
using Content.Shared.Clothing;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion;
using Content.Shared.Inventory;
using Content.Shared.Verbs;

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
        SubscribeLocalEvent<ModsuitPartComponent, InventoryRelayedEvent<GetExplosionResistanceEvent>>(
            RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, ClothingGotEquippedEvent>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, ClothingGotUnequippedEvent>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, AttemptEmpBreakWiresEvent>(RelayModsuitEvent);
        
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<Verb>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<ActivationVerb>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<AlternativeVerb>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<EquipmentVerb>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<ExamineVerb>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<InnateVerb>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<UtilityVerb>>(RelayModsuitEvent);

        // These are different because they just get called directly on the module, rather than raised wrapped in ModsuitRelayedEvent
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<Verb>>>(OnRelayedVerbsEvent);
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<ActivationVerb>>>(OnRelayedVerbsEvent);
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<AlternativeVerb>>>(OnRelayedVerbsEvent);
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<EquipmentVerb>>>(OnRelayedVerbsEvent);
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<ExamineVerb>>>(OnRelayedVerbsEvent);
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<InnateVerb>>>(OnRelayedVerbsEvent);
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<UtilityVerb>>>(OnRelayedVerbsEvent);
    }
    
    private void RelayModsuitEvent<T>(Entity<ModsuitPartComponent> entity, ref T args)
    {
        EntityUid controller = entity.Comp.Controller;
        
        var evt = new ModsuitRelayedEvent<T>(args, entity);
        
        // Modules might move or delete themselves in response to events so copy the list.
        var modules = _moduleSystem.GetContainedModules(controller).ToList();
        foreach (var module in modules)
        {
            RaiseLocalEvent(module, evt);
        }
        
        args = evt.Args;
    }

    private void OnRelayedVerbsEvent<T>(EntityUid entity, ModsuitRelayVerbsComponent _, ref ModsuitRelayedEvent<GetVerbsEvent<T>> evt)
        where T: Verb
    {
        if (!PartMatchesModule(entity, evt.Owner) || !CanModuleBeEnabled(entity))
            return;
        
        var args = evt.Args;
        RaiseLocalEvent(entity, args);
    }
}