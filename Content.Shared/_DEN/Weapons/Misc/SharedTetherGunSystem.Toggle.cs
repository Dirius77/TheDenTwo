using Content.Shared.Item.ItemToggle.Components;

// ReSharper disable once CheckNamespace
namespace Content.Shared.Weapons.Misc;

public abstract partial class SharedTetherGunSystem
{
    private void InitializeToggle()
    {
        // Make it so if some external system toggles the gun it drops the item.
        SubscribeLocalEvent<TetherGunComponent, ItemToggledEvent>(OnTetherToggle);
        SubscribeLocalEvent<ForceGunComponent, ItemToggledEvent>(OnForceToggle);
    }

    private void OnForceToggle(Entity<ForceGunComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated)
            StopTether(ent.Owner, ent.Comp);
    }

    private void OnTetherToggle(Entity<TetherGunComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated)
            StopTether(ent.Owner, ent.Comp);
    }
}