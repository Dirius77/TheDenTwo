using System.Diagnostics;
using System.Linq;
using Content.Shared._DEN.Clothing.Sealable.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._DEN.Clothing.Sealable.EntitySystems;

public abstract partial class SharedSealableClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly LocId _cantToggleReason = "sealable-clothing-cant-toggle-sealed";
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SealableClothingComponent, GotUnequippedEvent>(OnSealableUnequipped);
        SubscribeLocalEvent<SealableClothingComponent, BeingUnequippedAttemptEvent>(OnBeingUnequipped);
        SubscribeLocalEvent<SealableClothingComponent, ToggleSealDoAfterEvent>(OnToggleSealDoAfter);
        SubscribeLocalEvent<SealableClothingComponent, GetVerbsEvent<EquipmentVerb>>(OnGetVerbs);
        SubscribeLocalEvent<SealableClothingComponent, ToggleClothingAttemptEvent>(OnToggleClothingAttempt);
    }

    private void OnToggleClothingAttempt(Entity<SealableClothingComponent> entity, ref ToggleClothingAttemptEvent evt)
    {
        if (entity.Comp.IsSealed)
        {
            evt.Reason = _cantToggleReason;
            evt.Cancel();
        }
    }

    private void OnBeingUnequipped(Entity<SealableClothingComponent> entity, ref BeingUnequippedAttemptEvent evt)
    {
        if (entity.Comp.IsSealed)
            evt.Cancel();
    }

    private void OnSealableUnequipped(Entity<SealableClothingComponent> entity, ref GotUnequippedEvent args)
    {
        // Sealed clothing is only intended to be so while worn. This makes sure that if the entity somehow gets
        // force unequipped it doesn't stay stuck in a sealed state.
        if (entity.Comp.IsSealed)
            ModifyClothingSeal(entity.AsNullable(), false);
    }

    private void OnToggleSealDoAfter(Entity<SealableClothingComponent> entity, ref ToggleSealDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        
        ModifyClothingSeal(entity.AsNullable(), !entity.Comp.IsSealed);
    }

    private void OnGetVerbs(Entity<SealableClothingComponent> entity, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;
        
        var user = args.User;
        
        var verb = new EquipmentVerb()
        {
            Text = Loc.GetString("sealable-clothing-verb-text"),
            Act = () => TryToggleSeal(user, entity.AsNullable(), out _),
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Causes a user to start the doafter toggling the seal on the passed entity.
    /// </summary>
    /// <param name="user">The user who is trying to seal the clothing.</param>
    /// <param name="entity">The piece of clothing being sealed.</param>
    /// <param name="doAfterId">The DoAfterId of the created DoAfter.</param>
    [PublicAPI]
    public void TryToggleSeal(EntityUid user, Entity<SealableClothingComponent?> entity, out DoAfterId? doAfterId)
    {
        doAfterId = null;
        
        if (!Resolve(entity, ref entity.Comp))
            return;
        
        var evt = new ChangeSealStateAttemptEvent(!entity.Comp.IsSealed);
        RaiseLocalEvent(entity, evt);
        if (evt.Cancelled)
            return;
        
        // The sealing can continue while doing anything else, however, only one piece can seal or unseal at a time.
        var args = new DoAfterArgs(EntityManager, user, entity.Comp.SealDoAfterTime, new ToggleSealDoAfterEvent(), entity, user, entity)
        {
            BreakOnDamage = false,
            BreakOnMove = false,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (!_doAfter.TryStartDoAfter(args, out doAfterId))
            return;
        
        var popup = entity.Comp.IsSealed 
            ? Loc.GetString(entity.Comp.UnsealMessage, ("entity", Name(entity))) 
            : Loc.GetString(entity.Comp.SealMessage, ("entity", Name(entity)));
        
        _popupSystem.PopupPredicted(popup, user, user, PopupType.Medium);
    }

    /// <summary>
    /// Checks if a piece of clothing is sealed.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>The sealed state, or false if it is not Sealable</returns>
    [PublicAPI]
    public bool IsSealed(Entity<SealableClothingComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;
        
        return entity.Comp.IsSealed;
    }
    
    private void ModifyClothingSeal(Entity<SealableClothingComponent?> entity, bool state)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        
        if (!Resolve(entity, ref entity.Comp))
            return;
        
        var hasChange = entity.Comp.IsSealed != state;
        entity.Comp.IsSealed = state;
        // Don't do work if the new state isn't different.
        if (!hasChange)
            return;

        ComponentRegistry addingComps = new();
        ComponentRegistry removingComps = new();
        if (state)
        {
            foreach (var entry in entity.Comp.SealedAddComponents ?? [])
            {
                addingComps[entry.Key] = entry.Value;
            }

            // Unsealed is removed first because these are 'implicit' and we want the explict ones from the yml to take
            // priority
            foreach (var entry in entity.Comp.UnsealedAddComponents ?? [])
            {
                removingComps[entry.Key] = entry.Value;
            }
            
            foreach (var entry in entity.Comp.SealedRemoveComponents ?? [])
            {
                removingComps[entry.Key] = entry.Value;
            }
        }
        else
        {
            foreach (var entry in entity.Comp.UnsealedAddComponents ?? [])
            {
                addingComps[entry.Key] = entry.Value;
            }

            // Remove the Sealed Add Comps
            foreach (var entry in entity.Comp.SealedAddComponents ?? [])
            {
                removingComps[entry.Key] = entry.Value;
            }
            
            foreach (var entry in entity.Comp.UnsealedRemoveComponents ?? [])
            {
                removingComps[entry.Key] = entry.Value;
            }
        }

        // Create the right event type for the state change.
        ClothingSealStateChangedEvent evt;
        if (state)
            evt = new ClothingSealedEvent(addingComps, removingComps);
        else
            evt = new ClothingUnsealedEvent(addingComps, removingComps);
        
        RaiseLocalEvent(entity, evt);
        
        // Apply all the changes handled in the event.
        EntityManager.AddComponents(entity, evt.AddingComponents);
        EntityManager.RemoveComponents(entity, evt.RemovingComponents);
        _appearance.SetData(entity, SealableClothingVisuals.State, state);
        _item.VisualsChanged(entity);
        
        DirtyEntity(entity);
    }
}

[Serializable, NetSerializable]
public enum SealableClothingVisuals : byte
{
    State
}

public abstract class ClothingSealStateChangedEvent(ComponentRegistry addingComponents, ComponentRegistry removingComponents)
{
    public ComponentRegistry AddingComponents = addingComponents;
    public ComponentRegistry RemovingComponents = removingComponents;
}

public sealed class ClothingSealedEvent(ComponentRegistry addingComponents, ComponentRegistry removingComponents)
    : ClothingSealStateChangedEvent(addingComponents, removingComponents);

public sealed class ClothingUnsealedEvent(ComponentRegistry addingComponents, ComponentRegistry removingComponents)
    : ClothingSealStateChangedEvent(addingComponents, removingComponents);

/// <summary>
/// Raised BEFORE the doAfter to change the state of a piece of Sealable Clothing is created. Cancel to prevent it.
/// </summary>
/// <param name="goalState">If the clothing is being sealed or unsealed</param>
[Serializable, NetSerializable]
public sealed class ChangeSealStateAttemptEvent(bool goalState) : CancellableEntityEventArgs
{
    public readonly bool GoalState = goalState;
}

[Serializable, NetSerializable]
public sealed partial class ToggleSealDoAfterEvent : SimpleDoAfterEvent;