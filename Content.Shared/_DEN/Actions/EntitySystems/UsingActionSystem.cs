using Content.Shared._DEN.Actions.Components;
using Content.Shared.Interaction;

namespace Content.Shared._DEN.Actions.EntitySystems;

public sealed class UsingActionSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<InteractUsingActionComponent, InteractUsingActionEvent>(OnInteractAction);
        SubscribeLocalEvent<ActivateUsingActionComponent, ActivateUsingActionEvent>(OnActivateAction);
        SubscribeLocalEvent<InteractInWorldActionComponent, InteractInWorldActionEvent>(OnInteractInWorldAction);
    }

    private void OnInteractAction(Entity<InteractUsingActionComponent> entity, ref InteractUsingActionEvent args)
    {
        args.Handled = _interactionSystem.InteractUsing(args.Performer, entity, args.Target, Transform(args.Target).Coordinates);
    }
    
    private void OnActivateAction(Entity<ActivateUsingActionComponent> entity, ref ActivateUsingActionEvent args)
    {
        args.Handled = _interactionSystem.InteractionActivate(args.Performer, entity.Owner);
    }
    
    private void OnInteractInWorldAction(Entity<InteractInWorldActionComponent> entity, ref InteractInWorldActionEvent args)
    {
        args.Handled = true;
        var canReach = _interactionSystem.InRangeUnobstructed(args.Performer, args.Target);
        _interactionSystem.InteractUsingRanged(args.Performer, entity, args.Entity, args.Target, canReach);
    }
}