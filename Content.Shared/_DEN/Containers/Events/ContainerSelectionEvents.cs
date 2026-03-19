using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Containers.Events;

/// <summary>
///     Sent client -> server to tell the server we're trying to make a selection on an inventory.
/// </summary>
/// <param name="target">The target entity to make the selection on.</param>
/// <param name="selectionIndex">The index in the selection list that the user selected.</param>
[Serializable, NetSerializable]
public sealed class ContainerSelectionMessage(NetEntity target, int selectionIndex) : EntityEventArgs
{
    public readonly NetEntity Target = target;
    public readonly int SelectionIndex = selectionIndex;
}

[Serializable, NetSerializable]
public enum ContainerSelectionUiKey
{
    Key,
}
