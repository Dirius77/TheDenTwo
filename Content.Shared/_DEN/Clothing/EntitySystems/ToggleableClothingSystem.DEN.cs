// ReSharper disable once CheckNamespace
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
}

public sealed class ToggleClothingAttemptEvent : CancellableEntityEventArgs
{
    public LocId? Reason;
}