using Content.Shared._DEN.Containers.EntitySystems;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Containers.Components;

/// <summary>
///     Allows a container to be filled based on a selection that is provided to the user. Interactions will open
///     the selection UI instead of opening the container until a selection is made. Once a selection is made the
///     component effectively disables itself (it can't remove itself because the associated UI cannot be removed) and
///     the container will function as normal.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedContainerSelectionSystem))]
public sealed partial class EntityTableContainerSelectionComponent : Component
{
    // Whether a selection has already been made.
    [AutoNetworkedField, ViewVariables]
    public bool SelectionMade = false;

    /// <summary>
    ///     The list of possible selections for this Selection Component.
    /// </summary>
    [DataField]
    public List<ContainerSelectionEntry> Selections = new();
}

[DataDefinition]
public sealed partial class ContainerSelectionEntry
{
    /// <summary>
    ///     The name of this selection group, this is a localization string and the result will be shown to users.
    /// </summary>
    [DataField("name")]
    public LocId SelectionName;

    /// <summary>
    ///     Maps container names to <see cref="EntityTableSelector"/>. These will be used to populate the container
    ///     once a selection is made.
    /// </summary>
    [DataField]
    public Dictionary<string, EntityTableSelector> Containers = new();
}
