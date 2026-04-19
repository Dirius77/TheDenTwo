using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared.Inventory;
using Content.Shared.PowerCell;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public sealed class ConsumePowerOnMeleeSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ConsumeModsuitPowerOnMeleeComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<ConsumeModsuitPowerOnMeleeComponent> ent, ref MeleeHitEvent args)
    {
        if ((!args.IsHit || args.HitEntities.Count == 0) && !ent.Comp.MissesConsumePower)
            return;

        if (!_inventory.TryGetSlotEntity(args.User, ent.Comp.ExpectedModsuitSlot, out var modsuit) ||
            !HasComp<ModsuitControllerComponent>(modsuit))
            return;

        if (!_powerCell.TryUseCharge(modsuit.Value, ent.Comp.DrainOnMelee, args.User, true))
        {
            args.Handled = true;
        }
    }
}