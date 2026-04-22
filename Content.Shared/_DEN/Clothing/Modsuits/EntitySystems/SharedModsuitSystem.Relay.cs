using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Sealable.EntitySystems;
using Content.Shared.Armor;
using Content.Shared.Damage.Systems;
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
        
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<Verb>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<ActivationVerb>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<AlternativeVerb>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<EquipmentVerb>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<ExamineVerb>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<InnateVerb>>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, GetVerbsEvent<UtilityVerb>>(RelayModsuitEvent);

        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<Verb>>>(OnRelayedVerbsEvent);
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<ActivationVerb>>>(OnRelayedVerbsEvent);
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<AlternativeVerb>>>(OnRelayedVerbsEvent);
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<EquipmentVerb>>>(OnRelayedVerbsEvent);
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<ExamineVerb>>>(OnRelayedVerbsEvent);
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<InnateVerb>>>(OnRelayedVerbsEvent);
        SubscribeLocalEvent<ModsuitRelayVerbsComponent, ModsuitRelayedEvent<GetVerbsEvent<UtilityVerb>>>(OnRelayedVerbsEvent);

        
    }
    
    protected void RelayModsuitEvent<T>(Entity<ModsuitPartComponent> entity, ref T args)
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

    private void OnRelayedVerbsEvent<T>(EntityUid entity, ModsuitRelayVerbsComponent _, ref ModsuitRelayedEvent<GetVerbsEvent<T>> evt)
        where T: Verb
    {
        if (!PartMatchesModule(entity, evt.Owner) || !CanModuleBeEnabled(entity))
            return;
        
        var args = evt.Args;
        RaiseLocalEvent(entity, args);
    }
}