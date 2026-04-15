using System.Diagnostics.CodeAnalysis;
using Content.Shared.Clothing.Components;

namespace Content.Shared.Clothing.EntitySystems;

public sealed partial class ToggleableClothingSystem
{
    private bool CanToggleClothing(EntityUid targetClothing, out LocId? reason)
    {
        var evt = new ToggleClothingAttemptEvent();
        RaiseLocalEvent(targetClothing, evt);
        reason = evt.Reason;
        return !evt.Cancelled;
    }

    public bool TryGetClothingSlot(Entity<ToggleableClothingComponent> entity, string slot,
        [NotNullWhen(true)] out EntityUid? clothing)
    {
        clothing = null;

        foreach (var kvp in entity.Comp.ClothingUids)
        {
            if (kvp.Value == slot)
            {
                clothing = kvp.Key;
                return true;
            }
        }

        return false;
    }
}

public sealed class ToggleClothingAttemptEvent : CancellableEntityEventArgs
{
    public LocId? Reason;
}