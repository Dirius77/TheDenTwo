using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Holosign.Events;

[Serializable, NetSerializable]
public enum LabelableHolosignUIKey
{
    Signs,
    Description,
}

/// <summary>
/// Sent from the client to the server to inform the server that the user has set a new description for the labeler.
/// </summary>
/// <param name="description">The description text.</param>
/// <param name="isNsfw">If the user marked the description text as requiring NSFWDescriptions consent.</param>
[Serializable, NetSerializable]
public sealed class LabelableHolosignDescriptionMessage(string description, bool isNsfw) : BoundUserInterfaceMessage
{
    public string Description { get; } = description;
    public bool IsNsfw { get; } = isNsfw;
}

/// <summary>
/// Sent from the client to the server to request setting a labelable holoprojector to use the selected prototype.
/// </summary>
/// <param name="selection">The index in the prototype list to use.</param>
[Serializable, NetSerializable]
public sealed class LabelableHolosignSignChosen(int selection) : BoundUserInterfaceMessage
{
    public int Selection { get; } = selection;
}

/// <summary>
/// Sent from the client to the server to request opening the description editing interface, instead of the default
/// radial menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class LabelableHolosignOpenOtherUI : BoundUserInterfaceMessage;
