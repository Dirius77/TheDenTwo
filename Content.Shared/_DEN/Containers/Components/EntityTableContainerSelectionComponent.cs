using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Containers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityTableContainerSelectionComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public bool SelectionMade = false;

    [DataField]
    public List<ContainerSelectionEntry> Selections = new();
}

[DataDefinition]
public sealed partial class ContainerSelectionEntry
{
    [DataField("name")]
    public LocId SelectionName;

    [DataField]
    public Dictionary<string, EntityTableSelector> Containers = new();
}
