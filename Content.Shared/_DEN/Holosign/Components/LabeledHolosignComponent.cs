using Content.Shared._DEN.Holosign.Systems;
using Robust.Shared.GameStates;


namespace Content.Shared._DEN.Holosign.Components;

/// <summary>
/// Adds a description to the examine menu of the attached entity, optionally requiring the examine detail to require
/// the user to have the NSFWDescriptions consent in order to view it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedLabelableHolosignProjectorSystem))]
public sealed partial class LabeledHolosignComponent : Component
{
    /// <summary>
    /// The actual examine text to attach to the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Description;

    /// <summary>
    /// Determines if the examine text requires NSFWDescriptions consent in order to be visible. A fallback message
    /// explaining that the consent is not turned on is displayed otherwise.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsNSFW;
}
