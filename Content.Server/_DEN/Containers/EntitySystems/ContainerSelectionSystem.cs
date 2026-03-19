using System.Linq;
using System.Numerics;
using Content.Shared._DEN.Containers.Components;
using Content.Shared._DEN.Containers.EntitySystems;
using Content.Shared._DEN.Containers.Events;
using Content.Shared.ActionBlocker;
using Content.Shared.Destructible;
using Content.Shared.EntityTable;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._DEN.Containers.EntitySystems;

public sealed partial class ContainerSelectionSystem : SharedContainerSelectionSystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ActionBlockerSystem _blockerSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ContainerSelectionMessage>(OnContainerSelectionMessage);

        SubscribeLocalEvent<EntityTableContainerSelectionComponent, DestructionEventArgs>(OnDestruction, before: [typeof(SharedStorageSystem)]);
    }

    private void OnContainerSelectionMessage(ContainerSelectionMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } user)
            return;

        var targetEnt = GetEntity(message.Target);
        if (!TryComp<EntityTableContainerSelectionComponent>(targetEnt, out var comp))
            return;

        if (!_blockerSystem.CanInteract(user, targetEnt))
            return;

        if (comp.Selections.Count < message.SelectionIndex)
            return;

        var selection = comp.Selections[message.SelectionIndex];
        OnSelectionMade((targetEnt, comp), selection);
    }

    private void OnDestruction(Entity<EntityTableContainerSelectionComponent> ent,
        ref DestructionEventArgs args)
    {
        Log.Debug("Got Destruction Event.");

        OnSelectionMade(ent, _random.Pick(ent.Comp.Selections));
    }

    private void OnSelectionMade(Entity<EntityTableContainerSelectionComponent> ent,
        ContainerSelectionEntry selection)
    {
        if (TerminatingOrDeleted(ent) || !Exists(ent))
            return;

        if (ent.Comp.SelectionMade)
            return;

        if (!TryComp(ent, out ContainerManagerComponent? containerComp))
            return;

        var xform = Transform(ent);
        var coords = new EntityCoordinates(ent, Vector2.Zero);

        foreach (var (containerId, table) in selection.Containers)
        {
            if (!_containers.TryGetContainer(ent, containerId, out var container, containerComp))
            {
                Log.Error($"Entity {ToPrettyString(ent)} with a {nameof(EntityTableContainerSelectionComponent)} is missing a container ({containerId}).");
                continue;
            }

            var spawns = _entityTable.GetSpawns(table);
            foreach (var proto in spawns)
            {
                var spawn = Spawn(proto, coords);
                if (!_containers.Insert(spawn, container, containerXform: xform))
                {
                    var alreadyContained = container.ContainedEntities.Count > 0
                        ? string.Join("\n", container.ContainedEntities.Select(e => $"\t - {ToPrettyString(e)}"))
                        : "< empty >";
                    Log.Error($"Entity {ToPrettyString(ent)} with a {nameof(EntityTableContainerSelectionComponent)} failed to insert an entity: {ToPrettyString(spawn)}.\nCurrent contents:\n{alreadyContained}");
                    _transform.AttachToGridOrMap(spawn);
                    break;
                }
            }
        }

        _uiSystem.CloseUi(ent.Owner, ContainerSelectionUiKey.Key);
        ent.Comp.SelectionMade = true;
        Dirty(ent, ent.Comp);
    }
}
