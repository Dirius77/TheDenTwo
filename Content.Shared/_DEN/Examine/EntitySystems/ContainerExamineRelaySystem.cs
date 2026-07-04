using Content.Shared._DEN.Examine.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Robust.Shared.Containers;

namespace Content.Shared._DEN.Examine.EntitySystems;

public sealed partial class ContainerExamineRelaySystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<ContainerExamineRelayComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RelayedSolutionExamineComponent, ContainedRelayEvent<ExaminedEvent>>(OnSolutionExamineRelay);
    }

    private void OnExamined(Entity<ContainerExamineRelayComponent> entity, ref ExaminedEvent args)
    {
        if (!_containerSystem.TryGetContainingContainer(entity.Owner, out var container))
            return;
        
        var parent = container.Owner;
        var evt = new ContainedRelayEvent<ExaminedEvent>(args, entity);
        RaiseLocalEvent(parent, evt);
    }

    private void OnSolutionExamineRelay(Entity<RelayedSolutionExamineComponent> entity,
        ref ContainedRelayEvent<ExaminedEvent> evt)
    {
        if (!_solutionSystem.TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution))
            return;

        var args = evt.Args;
        args.PushMessage(_solutionSystem.GetSolutionExamine(solution));
    }
}