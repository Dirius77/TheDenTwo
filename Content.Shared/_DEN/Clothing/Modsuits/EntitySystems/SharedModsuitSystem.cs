using System.Diagnostics.CodeAnalysis;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Sealable.Components;
using Content.Shared._DEN.Clothing.Sealable.EntitySystems;
using Content.Shared._DEN.Modules.Components;
using Content.Shared._DEN.Modules.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public abstract partial class SharedModsuitSystem : EntitySystem
{
    [Dependency] private readonly SharedModuleStorageSystem _moduleSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
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
        
        SubscribeLocalEvent<ModsuitModuleComponent, ModuleInsertedEvent>(OnModsuitModuleInserted);
        SubscribeLocalEvent<ModsuitModuleComponent, ModuleRemovedEvent>(OnModsuitModuleRemoved);

        SubscribeLocalEvent<ModsuitPartAttachedModuleComponent, ModuleInsertedEvent>(OnPartAttachedModuleInserted);
        SubscribeLocalEvent<ModsuitPartAttachedModuleComponent, ModuleRemovedEvent>(OnPartAttachedModuleRemoved);

        SubscribeLocalEvent<ModsuitControllerComponent, GetItemActionsEvent>(OnControllerItemActions);
        SubscribeLocalEvent<ModsuitControllerComponent, ModsuitControllerOpenUiEvent>(OnControllerOpenUi);
        SubscribeLocalEvent<ModsuitControllerComponent, ModsuitToggleModuleMessage>(OnModsuitModuleToggle);

        SubscribeLocalEvent<ModsuitPartComponent, ClothingSealedEvent>(RelayModsuitEvent);
        SubscribeLocalEvent<ModsuitPartComponent, ClothingUnsealedEvent>(RelayModsuitEvent);
        
        SubscribeLocalEvent<ModsuitPassiveComponentModuleComponent, ModsuitRelayedEvent<ClothingSealedEvent>>(OnPassiveComponentSealed);
        SubscribeLocalEvent<ModsuitPassiveComponentModuleComponent, ModsuitRelayedEvent<ClothingUnsealedEvent>>(OnPassiveComponentUnsealed);
    }

    private void OnPartAttachedModuleInserted(Entity<ModsuitPartAttachedModuleComponent> entity,
        ref ModuleInsertedEvent args)
    {
        if (TryComp<ToggleableClothingComponent>(args.Storage, out var toggleClothingComp))
        {
            foreach (var child in toggleClothingComp.ClothingUids)
            {
                var slot = child.Value;
                if (entity.Comp.Slots.Contains(slot))
                {
                    entity.Comp.AttachedParts[slot] = child.Key;
                }
            }
        }
    }
    
    private void OnPartAttachedModuleRemoved(Entity<ModsuitPartAttachedModuleComponent> entity,
        ref ModuleRemovedEvent args)
    {
        entity.Comp.AttachedParts.Clear();
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
            evt.Handled = _uiSystem.TryOpenUi(entity.Owner, ModsuitControllerUiKey.Key, evt.Performer);
        }
    }

    // I need this helper please god.
    private bool IsWorn(EntityUid entity)
    {
        return _clothingQuery.TryComp(entity, out var clothing)
               && clothing.InSlotFlag is { } slotFlag
               && clothing.Slots.HasFlag(slotFlag);
    }

    public bool ModuleHasActiveController(Entity<ModsuitModuleComponent?> entity,
        [NotNullWhen(true)] out Entity<ModsuitControllerComponent>? controller)
    {
        controller = null;
        if (!Resolve(entity, ref entity.Comp))
            return false;
        
        if (entity.Comp.ModController is not { } controlEnt
            || !IsActiveController(controlEnt))
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

    public bool TryGetPartFromController(Entity<ModsuitControllerComponent> controller, string slot,
        [NotNullWhen(true)] out Entity<ModsuitPartComponent>? modsuitPart)
    {
        modsuitPart = null;
        if (!IsWorn(controller))
            return false;
        
        var wearer = Transform(controller).ParentUid;
        if (!_inventorySystem.TryGetSlotEntity(wearer, slot, out var entity))
            return false;

        if (!TryComp<ModsuitPartComponent>(entity, out var partComp))
            return false;
        
        modsuitPart = (entity.Value, partComp);
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
            foreach (var part in attachedPartComp.AttachedParts)
            {
                if (!TryGetPartFromController(controller.Value, part.Key, out var modsuitPart)
                    || !_sealableSystem.IsSealed(modsuitPart.Value.Owner))
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

    private void OnPassiveComponentSealed(Entity<ModsuitPassiveComponentModuleComponent> entity,
        ref ModsuitRelayedEvent<ClothingSealedEvent> evt)
    {
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

    private void OnModsuitModuleInserted(Entity<ModsuitModuleComponent> entity, ref ModuleInsertedEvent args)
    {
        if (TryComp<ModsuitControllerComponent>(args.Storage, out var controller))
        {
            entity.Comp.ModController = (args.Storage, controller);
        }
    }

    private void OnModsuitModuleRemoved(Entity<ModsuitModuleComponent> entity, ref ModuleRemovedEvent args)
    {
        entity.Comp.ModController = null;
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