using System.Diagnostics.CodeAnalysis;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Sealable.Components;
using Content.Shared._DEN.Clothing.Sealable.EntitySystems;
using Content.Shared._DEN.Modules.Components;
using Content.Shared._DEN.Modules.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public abstract partial class SharedModsuitSystem : EntitySystem
{
    [Dependency] private readonly SharedModuleStorageSystem _moduleSystem = default!;
    [Dependency] private readonly SealableClothingSystem _sealableSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ItemToggleSystem _toggleSystem = default!;
    
    private EntityQuery<AttachedClothingComponent> _attachedClothingQuery;
    private EntityQuery<ModsuitControllerComponent> _modsuitControllerQuery;
    private EntityQuery<ClothingComponent> _clothingQuery;
    
    public override void Initialize()
    {
        _attachedClothingQuery = GetEntityQuery<AttachedClothingComponent>();
        _modsuitControllerQuery = GetEntityQuery<ModsuitControllerComponent>();
        _clothingQuery = GetEntityQuery<ClothingComponent>();

        InitializeRelay();
        
        SubscribeLocalEvent<ModsuitModuleComponent, ModuleInsertedEvent>(OnModsuitModuleInserted);
        SubscribeLocalEvent<ModsuitModuleComponent, ModuleRemovedEvent>(OnModsuitModuleRemoved);

        SubscribeLocalEvent<ModsuitControllerComponent, GetItemActionsEvent>(OnControllerItemActions);
        SubscribeLocalEvent<ModsuitControllerComponent, MapInitEvent>(OnControllerInit, after: [typeof(ToggleableClothingSystem)]);
        SubscribeLocalEvent<ModsuitControllerComponent, ModsuitControllerOpenUiEvent>(OnControllerOpenUi);
        SubscribeLocalEvent<ModsuitControllerComponent, ModsuitToggleModuleMessage>(OnModsuitModuleToggle);
        SubscribeLocalEvent<ModsuitControllerComponent, GotUnequippedEvent>(OnModsuitUnequipped);
        
        SubscribeLocalEvent<ModsuitPassiveComponentModuleComponent, ModsuitRelayedEvent<ClothingSealedEvent>>(OnPassiveComponentSealed);
        SubscribeLocalEvent<ModsuitPassiveComponentModuleComponent, ModsuitRelayedEvent<ClothingUnsealedEvent>>(OnPassiveComponentUnsealed);
    }

    private void OnModsuitModuleToggle(Entity<ModsuitControllerComponent> entity, ref ModsuitToggleModuleMessage msg)
    {
        if (!TryComp<ModuleStorageComponent>(entity, out var storage))
            return;

        var target = GetEntity(msg.Target);

        if (!storage.ModuleContainer.Contains(target))
            return;

        _toggleSystem.TrySetActive(target, msg.State, msg.Actor);
        UpdateUI(entity);
    }

    private void OnControllerItemActions(Entity<ModsuitControllerComponent> entity, ref GetItemActionsEvent evt)
    {
        if (evt.InHands)
            return;
        
        evt.AddAction(ref entity.Comp.UiActionEntity, entity.Comp.ActionId);
    }

    private void OnControllerOpenUi(Entity<ModsuitControllerComponent> entity, ref ModsuitControllerOpenUiEvent evt)
    {
        if (_uiSystem.HasUi(entity, ModsuitControllerUiKey.Key))
        {
            if (_uiSystem.IsUiOpen(entity.Owner, ModsuitControllerUiKey.Key, evt.Performer))
            {
                _uiSystem.CloseUi(entity.Owner, ModsuitControllerUiKey.Key, evt.Performer);
                evt.Handled = true;
            }
            else
            {
                evt.Handled = _uiSystem.TryOpenUi(entity.Owner, ModsuitControllerUiKey.Key, evt.Performer);
            }
        }
    }

    // I need this helper please god.
    private bool IsWorn(EntityUid entity)
    {
        return _clothingQuery.TryComp(entity, out var clothing)
               && clothing.InSlotFlag is { } slotFlag
               && clothing.Slots.HasFlag(slotFlag);
    }
    
    public bool TryGetModuleController(Entity<ModsuitModuleComponent?> entity,
        [NotNullWhen(true)] out Entity<ModsuitControllerComponent>? controller)
    {
        controller = null;
        if (!Resolve(entity, ref entity.Comp))
            return false;
        
        if (entity.Comp.ModController is not { } controlEnt)
            return false;

        if (!TryComp<ModsuitControllerComponent>(controlEnt, out var controllerComp))
            return false;
        
        controller = (controlEnt, controllerComp);
        return true;
    }

    public bool ModuleHasActiveController(Entity<ModsuitModuleComponent?> entity,
        [NotNullWhen(true)] out Entity<ModsuitControllerComponent>? controller)
    {
        controller = null;
        if (!Resolve(entity, ref entity.Comp))
            return false;
        
        if (!TryGetModuleController(entity, out var controlEnt)
            || !IsActiveController(controlEnt.Value))
            return false;

        controller = controlEnt;
        return true;
    }

    private bool IsActiveController(Entity<ModsuitControllerComponent> controller)
    {
        if (!IsWorn(controller))
            return false;

        return _sealableSystem.IsSealed(controller.Owner);
    }

    public bool CanPartBeActivated(Entity<ModsuitPartComponent?> part)
    {
        if (!Resolve(part, ref part.Comp))
            return false;

        if (!IsWorn(part))
            return false;
        
        if (!_sealableSystem.IsSealed(part.Owner))
            return false;
        
        return true;
    }

    public bool CanModuleBeEnabled(Entity<ModsuitModuleComponent?> entity)
    {
        if (!ModuleHasActiveController(entity, out var controller))
            return false;

        // This defaults to true because if it doesn't have the comp, then all it needs is the controller.
        var seenOne = true;
        if (TryComp<ModsuitPartAttachedModuleComponent>(entity, out var attachedPartComp))
        {
            // Ok we have the comp, we have to find at least one part.
            seenOne = false;
            foreach (var part in GetModuleAttachedParts((entity, attachedPartComp)))
            {
                if (!_sealableSystem.IsSealed(part.Item2))
                {
                    if (attachedPartComp.NeedsAll)
                        return false;
                }
                else
                {
                    seenOne = true;
                }
            }
        }

        return seenOne;
    }

    public bool PartMatchesModule(Entity<ModsuitModuleComponent?> module, Entity<ModsuitPartComponent?> part)
    {
        if (!Resolve(module, ref module.Comp) || !Resolve(part, ref part.Comp))
            return false;

        // Module doesn't have a controller so no part matches.
        if (!TryGetModuleController(module, out var controller))
            return false;
        
        // If there's no part attached comp then EVERY part matches.
        if (!TryComp<ModsuitPartAttachedModuleComponent>(module.Owner, out var attachedPartComp))
            return true;

        // Do any of our slots match?
        foreach (var slot in attachedPartComp.Slots)
        {
            if (controller.Value.Comp.SlotToPart[slot] == part.Owner)
                return true;
        }
        
        return false;
    }

    public IReadOnlyList<(string, EntityUid)> GetModuleAttachedParts(Entity<ModsuitPartAttachedModuleComponent?> module)
    {
        if (!TryGetModuleController(module.Owner, out var controller))
            return [];

        if (!Resolve(module, ref module.Comp))
            return [];
        
        var result = new List<(string, EntityUid)>();
        foreach (var slot in module.Comp.Slots)
        {
            result.Add((slot, controller.Value.Comp.SlotToPart[slot]));
        }

        return result;
    }

    private void OnControllerInit(Entity<ModsuitControllerComponent> entity, ref MapInitEvent evt)
    {
        if (!TryComp<ToggleableClothingComponent>(entity, out var clothing))
        {
            Log.Warning($"{ToPrettyString(entity)} is a modsuit that is not also ToggleableClothing!");
            return;
        }

        entity.Comp.SlotToPart["back"] = entity;
        entity.Comp.PartToSlot[entity] = "back";
        // We actually just invert the list.
        foreach (var part in clothing.ClothingUids)
        {
            entity.Comp.SlotToPart[part.Value] = part.Key;
            entity.Comp.PartToSlot[part.Key] = part.Value;
        }
        Dirty(entity);
    }

    private void OnPassiveComponentSealed(Entity<ModsuitPassiveComponentModuleComponent> entity,
        ref ModsuitRelayedEvent<ClothingSealedEvent> evt)
    {
        if (!PartMatchesModule(entity.Owner, evt.Owner))
            return;
        
        var args = evt.Args;
        foreach (var comp in entity.Comp.BlockOnSeal ?? []) 
        { 
            args.AddingComponents.Remove(comp.Key);
        }
        
        foreach (var comp in entity.Comp.AddOnSeal ?? [])
        { 
            args.AddingComponents[comp.Key] = comp.Value;
        }
        
        foreach (var comp in entity.Comp.AddOnUnseal ?? [])
        {
            args.RemovingComponents[comp.Key] = comp.Value;
        }
    }
    
    private void OnPassiveComponentUnsealed(Entity<ModsuitPassiveComponentModuleComponent> entity,
        ref ModsuitRelayedEvent<ClothingUnsealedEvent> evt)
    {
        if (!PartMatchesModule(entity.Owner, evt.Owner))
            return;
        
        var args = evt.Args;
        foreach (var comp in entity.Comp.BlockOnUnseal ?? [])
        {
            args.AddingComponents.Remove(comp.Key);
        }
        
        foreach (var comp in entity.Comp.AddOnUnseal ?? [])
        {
            args.AddingComponents[comp.Key] = comp.Value;
        }

        foreach (var comp in entity.Comp.AddOnSeal ?? [])
        {
            args.RemovingComponents[comp.Key] = comp.Value;
        }
    }

    private void OnModsuitModuleInserted(Entity<ModsuitModuleComponent> entity, ref ModuleInsertedEvent args)
    {
        if (HasComp<ModsuitControllerComponent>(args.Storage))
        {
            entity.Comp.ModController = args.Storage;
            Dirty(entity);
        }
    }

    private void OnModsuitModuleRemoved(Entity<ModsuitModuleComponent> entity, ref ModuleRemovedEvent args)
    {
        entity.Comp.ModController = null;
        Dirty(entity);
    }

    public void TrySetSpringlocked(Entity<ModsuitControllerComponent> entity, bool locked)
    {
        // Relying on this feels kind of weird, but so does replicating its functionality everywhere.
        if (!TryComp<ToggleableClothingComponent>(entity, out var toggleable))
            return;
        
        foreach (var part in toggleable.ClothingUids)
        {
            var target = part.Key;
            // Doesn't make sense to seal it if it's not this, also if this somehow happened something has gone really
            // really wrong.
            if (HasComp<ModsuitPartComponent>(target) && HasComp<SealableClothingComponent>(target))
            {
                if (locked)
                    EnsureComp<SpringlockedComponent>(target);
                else
                    RemComp<SpringlockedComponent>(target);
            }
        }
        if (locked)
            EnsureComp<SpringlockedComponent>(entity);
        else
            RemComp<SpringlockedComponent>(entity);
        
        entity.Comp.PartsSpringlocked = locked;
        Dirty(entity);
    }

    public void ModuleUpdateUI(Entity<ModsuitModuleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (entity.Comp.ModController == null)
            return;

        if (!TryComp<ModsuitControllerComponent>(entity.Comp.ModController, out var modsuitController))
            return;
        
        UpdateUI((entity.Comp.ModController.Value, modsuitController));
    }

    private void OnModsuitUnequipped(Entity<ModsuitControllerComponent> entity, ref GotUnequippedEvent args)
    {
        _uiSystem.CloseUi(entity.Owner, ModsuitControllerUiKey.Key);
    }

    protected virtual void UpdateUI(Entity<ModsuitControllerComponent> entity)
    {
    }
}

[Serializable, NetSerializable]
public enum ModsuitControllerUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed partial class ModsuitToggleModuleMessage : BoundUserInterfaceMessage
{
    public NetEntity Target;
    public bool State;

    public ModsuitToggleModuleMessage(NetEntity target, bool state)
    {
        Target = target;
        State = state;
    }
}

public sealed class ModsuitRelayedEvent<TEvent> : EntityEventArgs
{
    public TEvent Args;

    public EntityUid Owner;

    public ModsuitRelayedEvent(TEvent args, EntityUid owner)
    {
        Args = args;
        Owner = owner;
    }
}

public sealed partial class ModsuitControllerOpenUiEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum SealableWireActionKey : byte
{
    StatusKey
}