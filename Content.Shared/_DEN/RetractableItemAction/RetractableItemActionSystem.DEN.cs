namespace Content.Shared.RetractableItemAction;

public sealed partial class RetractableItemActionSystem
{
    private void OnRetractableItemActionShutdown(Entity<RetractableItemActionComponent> entity, ref ComponentShutdown args)
    {
        if (entity.Comp.ActionItemUid is { } actionItem)
        {
            PredictedQueueDel(actionItem);
        }
    }
}