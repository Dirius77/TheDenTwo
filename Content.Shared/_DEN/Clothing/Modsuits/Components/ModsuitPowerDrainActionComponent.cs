using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

/// <summary>
/// Causes an action to consume power from a MODsuit before it can occur.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModsuitPowerDrainActionComponent : Component
{
    /// <summary>
    /// The amount of power that must be drained from the MODsuit for the action to occur.
    /// </summary>
    [DataField, AutoNetworkedField] public float DrainOnAction = 1.0f;

    /// <summary>
    /// The slot to look in for the modsuit.
    /// </summary>
    [DataField, AutoNetworkedField] public string ExpectedModsuitSlot = "back";
}