using Content.Shared._DEN.Actions.Components;
using Content.Shared.Interaction;

namespace Content.Shared._DEN.Actions.EntitySystems;

public sealed class InteractUsingActionSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<InteractUsingActionComponent, InteractUsingActionEvent>(OnInteractAction);
    }

    private void OnInteractAction(Entity<InteractUsingActionComponent> entity, ref InteractUsingActionEvent args)
    {
        args.Handled = _interactionSystem.InteractUsing(args.Performer, entity, args.Target, Transform(args.Target).Coordinates);
    }
}