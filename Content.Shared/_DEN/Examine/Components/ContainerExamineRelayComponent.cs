namespace Content.Shared._DEN.Examine.Components;

/// <summary>
/// Causes an examine event to also be passed to the container in the form of a ContainedRelayEvent.
/// </summary>
[RegisterComponent]
public sealed partial class ContainerExamineRelayComponent : Component;


public sealed class ContainedRelayEvent<TEvent> : EntityEventArgs
{
    public TEvent Args;

    public EntityUid Owner;

    public ContainedRelayEvent(TEvent args, EntityUid owner)
    {
        Args = args;
        Owner = owner;
    }
}