using System.Diagnostics.CodeAnalysis;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Modules.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public sealed class ConsumeModsuitPowerSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ModulePowerSystem _powerSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ConsumeModsuitPowerOnMeleeComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ModsuitPowerDrainActionComponent, ActionValidateEvent>(OnActionValidate, after: [typeof(SharedActionsSystem)]);
        SubscribeLocalEvent<ModsuitPowerDrainActionComponent, ActionPerformedEvent>(OnActionPerformed);
    }

    private bool TryGetModsuit(EntityUid user, string slot, [NotNullWhen(true)] out Entity<ModsuitControllerComponent>? modsuit)
    {
        modsuit = null;
        if (!_inventory.TryGetSlotEntity(user, slot, out var maybeModsuit) ||
            !TryComp<ModsuitControllerComponent>(maybeModsuit, out var modsuitComp))
            return false;
        modsuit = (maybeModsuit.Value, modsuitComp);
        return true;
    }

    private void OnMeleeHit(Entity<ConsumeModsuitPowerOnMeleeComponent> ent, ref MeleeHitEvent args)
    {
        if ((!args.IsHit || args.HitEntities.Count == 0) && !ent.Comp.MissesConsumePower)
            return;

        if (!TryGetModsuit(args.User, ent.Comp.ExpectedModsuitSlot, out var modsuit))
            return;

        if (!_powerSystem.TryUseCharge(modsuit.Value.Owner, ent.Comp.DrainOnMelee, args.User, true))
        {
            args.Handled = true;
        }
    }

    private void OnActionValidate(Entity<ModsuitPowerDrainActionComponent> ent, ref ActionValidateEvent args)
    {
        // Can't find a modsuit to get power from.
        if (!TryGetModsuit(args.User, ent.Comp.ExpectedModsuitSlot, out var modsuit))
        {
            args.Invalid = true;
            return;
        }
        
        if (!_powerSystem.HasCharge(modsuit.Value.Owner, ent.Comp.DrainOnAction, args.User, true))
        {
            args.Invalid = true;
        }
    }
    
    private void OnActionPerformed(Entity<ModsuitPowerDrainActionComponent> ent, ref ActionPerformedEvent args)
    {
        // Can't find a modsuit to get power from.
        if (!TryGetModsuit(args.Performer, ent.Comp.ExpectedModsuitSlot, out var modsuit))
            return;

        _powerSystem.TryUseCharge(modsuit.Value.Owner, ent.Comp.DrainOnAction, args.Performer, true);
    }
}