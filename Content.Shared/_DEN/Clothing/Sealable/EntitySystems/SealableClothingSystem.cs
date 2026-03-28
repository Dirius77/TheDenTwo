using Content.Shared._DEN.Clothing.Sealable.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory.Events;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Clothing.Sealable.EntitySystems;

public sealed partial class SealableClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SealableClothingComponent, GotUnequippedEvent>(OnSealableUnequipped);
    }

    private void OnSealableUnequipped(Entity<SealableClothingComponent> entity, ref GotUnequippedEvent args)
    {
        // Sealed clothing is only intended to be so while worn. This makes sure that if the entity somehow gets
        // force unequipped it doesn't stay stuck in a sealed state.
        if (entity.Comp.IsSealed)
            ModifyClothingSeal(entity.AsNullable(), false);
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

    /// <summary>
    /// Changes the Sealed state of the clothing.
    /// </summary>
    /// <param name="entity">The entity to change the state of.</param>
    /// <param name="state">The state to set sealed to.</param>
    [PublicAPI]
    public void ModifyClothingSeal(Entity<SealableClothingComponent?> entity, bool state)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;
        
        var evt = new ClothingSealStateChangedEvent(state);
        RaiseLocalEvent(entity, evt);

        // Don't do work if the new state isn't different.
        if (entity.Comp.IsSealed != state)
        {
            _appearance.SetData(entity, SealedClothingVisuals.State, entity.Comp.IsSealed);
            if (state && entity.Comp.SealedEquippedPrefix is { } prefix)
            {
                _clothingSystem.SetEquippedPrefix(entity.Owner, prefix);
            }
            else
            {
                _clothingSystem.SetEquippedPrefix(entity.Owner, null);
            }
        }
        
        entity.Comp.IsSealed = state;
    }
}

[Serializable, NetSerializable]
public enum SealedClothingVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public readonly record struct ClothingSealStateChangedEvent(bool IsSealed)
{
    public readonly bool IsSealed = IsSealed;
};