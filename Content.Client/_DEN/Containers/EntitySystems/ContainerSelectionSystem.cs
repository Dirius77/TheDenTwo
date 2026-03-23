using Content.Shared._DEN.Containers.EntitySystems;
using Content.Shared._DEN.Containers.Events;

namespace Content.Client._DEN.Containers.EntitySystems;

public sealed class ContainerSelectionSystem : SharedContainerSelectionSystem
{
    /// <summary>
    ///     Informs the server that a selection has been made on the container.
    /// </summary>
    /// <param name="entity">The entity being targeted.</param>
    /// <param name="containerIndex">The index in the selection list to choose.</param>
    public void SendSelectionEvent(EntityUid entity, int containerIndex)
    {
        RaiseNetworkEvent(new ContainerSelectionMessage(GetNetEntity(entity), containerIndex));
    }
}
