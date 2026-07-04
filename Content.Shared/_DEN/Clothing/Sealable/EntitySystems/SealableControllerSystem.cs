using System.Diagnostics.CodeAnalysis;
using Content.Shared._DEN.Clothing.Sealable.Components;
using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Timing;

namespace Content.Shared._DEN.Clothing.Sealable.EntitySystems;

public sealed partial class SealableControllerSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SealableClothingSystem _sealableClothing = default!;
    [Dependency] private IGameTiming _timing = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<SealableControllerComponent, ComponentRemove>(OnRemoveController);
        SubscribeLocalEvent<SealableControllerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SealableControllerComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<SealableControllerComponent, ToggleSealableControllerEvent>(OnToggleSealable);
        SubscribeLocalEvent<SealableControllerComponent, BeingUnequippedAttemptEvent>(OnControllerUnequipAttempt);
        
        SubscribeLocalEvent<SealedByControllerComponent, ToggleSealDoAfterEvent>(OnSealFinished, after: [typeof(SealableClothingSystem)]);
    }

    private void OnControllerUnequipAttempt(Entity<SealableControllerComponent> entity,
        ref BeingUnequippedAttemptEvent evt)
    {
        foreach (var slot in entity.Comp.ControlledSlots)
        {
            if (HasTarget(entity, slot, out var target) && _sealableClothing.IsSealed(target.Value.AsNullable()))
            {
                evt.Cancel();
                return;
            }
        }
    }

    private void OnToggleSealable(Entity<SealableControllerComponent> entity, ref ToggleSealableControllerEvent evt)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        
        // If we were in the process of toggling, stop.
        if (!entity.Comp.IsToggling)
        {
            entity.Comp.IsToggling = true;
            if (TryToggleNextPiece(entity, evt.Performer))
            {
                Dirty(entity, entity.Comp);
                return;
            }
        }

        // Either we're already toggling, or all of the clothing is in the right state already.
        entity.Comp.IsToggling = false;
        entity.Comp.CurrentSlot = 0;
        entity.Comp.Sealing = !entity.Comp.Sealing;
        Dirty(entity, entity.Comp);
    }

    /// <summary>
    /// Tries to find the next piece in the controlled set to toggle, incrementing CurrentSlot as it goes.
    /// </summary>
    /// <param name="entity">The controller entity.</param>
    /// <param name="user">The user causing this action to occur.</param>
    /// <returns></returns>
    private bool TryToggleNextPiece(Entity<SealableControllerComponent> entity, EntityUid user)
    {
        while (entity.Comp.CurrentSlot < entity.Comp.ControlledSlots.Count)
        {
            // Is there actually a sealable piece of clothing in this slot.
            var slot = entity.Comp.ControlledSlots[entity.Comp.CurrentSlot];
            if (HasTarget(entity, slot, out var target))
            {
                var comp = EnsureComp<SealedByControllerComponent>(target.Value);
                comp.Controller = entity;
                // Is this piece of clothing already sealed? (or not sealed, whichever we care about right now)
                if (entity.Comp.Sealing == _sealableClothing.IsSealed(target.Value.AsNullable()))
                {
                    entity.Comp.CurrentSlot++;
                    continue;
                }

                // We have a piece of clothing that is in the wrong state at this point.
                // This is delayed entirely for the aesthetics of letting the previous doafter bar despawn before starting the next one.
                Timer.Spawn(TimeSpan.FromMilliseconds(200), () => _sealableClothing.TryToggleSeal(user, target.Value.AsNullable()));
                entity.Comp.CurrentSlot++;
                return true;
            }

            entity.Comp.CurrentSlot++;
        }

        return false;
    }

    private void OnSealFinished(Entity<SealedByControllerComponent> entity, ref ToggleSealDoAfterEvent evt)
    {
        // Stop the doAfter bar from flickering (a little).
        if (!_timing.IsFirstTimePredicted)
            return;
        
        // Somehow an entity became sealed by a controller without a controller...
        if (entity.Comp.Controller is not { } controller)
            return;

        if (!TryComp<SealableControllerComponent>(controller, out var sealable))
            return;

        // Sealing got canceled, don't queue the next one.
        if (!sealable.IsToggling)
            return;
        
        // Try Toggling the next piece
        if (TryToggleNextPiece((controller, sealable), Transform(entity).ParentUid))
        {
            Dirty(controller, sealable);
            return;
        }
        
        // There's no other piece to toggle, so we're done, reset to a clean state.
        sealable.IsToggling = false;
        sealable.Sealing = !sealable.Sealing;
        sealable.CurrentSlot = 0;
        Dirty(controller, sealable);
    }

    /// <summary>
    /// Checks if the user is wearing a valid SealableClothing in the target slot.
    /// </summary>
    /// <param name="entity">The controller entity we're checking</param>
    /// <param name="slot">The slot to check for a SealableClothing</param>
    /// <param name="target">The entity found if there was one.</param>
    /// <returns>Whether a target was found</returns>
    private bool HasTarget(Entity<SealableControllerComponent> entity, string slot, [NotNullWhen(true)] out Entity<SealableClothingComponent>? target)
    {
        target = null;
        
        var parent = Transform(entity).ParentUid;
        if (!_inventory.TryGetSlotEntity(parent, slot, out var worn)) 
            return false;
        
        if (!TryComp<SealableClothingComponent>(worn, out var sealable)) 
            return false;
        
        target = (worn.Value, sealable);
        return true;

    }

    private void OnGetActions(Entity<SealableControllerComponent> entity, ref GetItemActionsEvent evt)
    {
        if (entity.Comp.ActionEntity != null 
            && (evt.SlotFlags & entity.Comp.RequiredFlags) == entity.Comp.RequiredFlags)
        {
            evt.AddAction(entity.Comp.ActionEntity);
        }
    }

    private void OnRemoveController(Entity<SealableControllerComponent> entity, ref ComponentRemove args)
    {
        _actionsSystem.RemoveAction(entity.Comp.ActionEntity);
    }

    private void OnMapInit(Entity<SealableControllerComponent> entity, ref MapInitEvent evt)
    {
        if (_actionContainer.EnsureAction(entity, ref entity.Comp.ActionEntity, out var action, entity.Comp.Action))
        {
            _actionsSystem.SetEntityIcon((entity.Comp.ActionEntity.Value, action), entity);
        }
    }
}

public sealed partial class ToggleSealableControllerEvent : InstantActionEvent;