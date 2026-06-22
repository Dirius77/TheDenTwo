using Content.Shared._DEN.Containers.Components;
using Content.Shared.UserInterface;

namespace Content.Shared._DEN.Containers.EntitySystems;

public abstract partial class SharedContainerSelectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityTableContainerSelectionComponent, ActivatableUIOpenAttemptEvent>(
            OnUserOpenUIAttempt);
    }

    private void OnUserOpenUIAttempt(Entity<EntityTableContainerSelectionComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (ent.Comp.SelectionMade)
            args.Cancel();
    }
}
