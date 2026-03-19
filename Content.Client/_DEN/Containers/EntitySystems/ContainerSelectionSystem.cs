using Content.Shared._DEN.Containers.EntitySystems;
using Content.Shared._DEN.Containers.Events;

namespace Content.Client._DEN.Containers.EntitySystems;

public sealed class ContainerSelectionSystem : SharedContainerSelectionSystem
{
    public void SendSelectionEvent(EntityUid entity, int containerIndex)
    {
        RaiseNetworkEvent(new ContainerSelectionMessage(GetNetEntity(entity), containerIndex));
    }
}
