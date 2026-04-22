namespace Content.Shared._DEN.Examine.Components;

/// <summary>
/// Handles relayed examine events from ContainerExamineRelayComponent and adds solution examine information to them.
/// </summary>
[RegisterComponent]
public sealed partial class RelayedSolutionExamineComponent : Component
{
    [DataField(required: true)] public string Solution;
}