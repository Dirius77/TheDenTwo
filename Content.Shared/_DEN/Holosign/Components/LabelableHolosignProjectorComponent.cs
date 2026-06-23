using Content.Shared._DEN.Holosign.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Holosign.Components;

/// <summary>
/// Gives an entity the ability to configure a label, as well as which from a list of entity prototypes it will spawn.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedLabelableHolosignProjectorSystem))]
public sealed partial class LabelableHolosignProjectorComponent : Component
{
    /// <summary>
    /// The entity to spawn with this projector.
    /// </summary>
    [DataField(required: true), Access(Other = AccessPermissions.ReadWriteExecute)]
    public List<EntProtoId> SignProtos;

    /// <summary>
    /// The currently in use sign prototype, null if one hasn't been chosen.
    /// </summary>
    [DataField, AutoNetworkedField] public EntProtoId? SelectedSignProto;

    /// <summary>
    /// The whitelist used to determine what entity is considered a valid holosign, allows the projector to pick the
    /// sign back up.
    /// </summary>
    [DataField]
    public EntityWhitelist SignWhitelist;

    /// <summary>
    /// Whether the holoprojector should use charges at all.
    /// </summary>
    [DataField]
    public bool UsesCharges = false;

    /// <summary>
    /// The currently set description text from the user that will be attached to the spawned entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), Access(Other = AccessPermissions.ReadWriteExecute)]
    [DataField, AutoNetworkedField]
    public string BarrierDescription = string.Empty;

    /// <summary>
    /// The maximum length of a description that can be attached to a barrier.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public int MaxDescriptionChars = 512;

    /// <summary>
    /// Whether the description text requires the NSFWDescriptions consent in order to view.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsNsfw;
}
