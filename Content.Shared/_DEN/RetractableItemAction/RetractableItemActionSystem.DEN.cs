using Robust.Shared.Network;

namespace Content.Shared.RetractableItemAction;

public sealed partial class RetractableItemActionSystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    
    private void OnRetractableItemActionShutdown(Entity<RetractableItemActionComponent> entity, ref ComponentShutdown args)
    {
        if (entity.Comp.ActionItemUid is { } actionItem)
        {
            PredictedQueueDel(actionItem);
        }
    }
}