using System.Linq;
using System.Numerics;
using Content.Shared._DEN.Containers.Components;
using Content.Shared._DEN.Containers.EntitySystems;
using Content.Shared._DEN.Containers.Events;
using Content.Shared.ActionBlocker;
using Content.Shared.Destructible;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
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

        SubscribeLocalEvent<EntityTableContainerSelectionComponent, DestructionEventArgs>(OnDestruction,
            before: [typeof(SharedStorageSystem)]);
    }

    private void OnContainerSelectionMessage(ContainerSelectionMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } user)
            return;

        // Is the targeted entity actually one that has an EntityTableContainerSelectionComponent?
        var targetEnt = GetEntity(message.Target);
        if (!TryComp<EntityTableContainerSelectionComponent>(targetEnt, out var comp))
            return;

        // Can the user even reach the container anymore?
        if (!_blockerSystem.CanInteract(user, targetEnt))
            return;

        // Don't allow invalid selections.
        if (comp.Selections.Count < message.SelectionIndex)
            return;

        var selection = comp.Selections[message.SelectionIndex];
        OnSelectionMade((targetEnt, comp), selection);
    }

    private void OnDestruction(Entity<EntityTableContainerSelectionComponent> ent,
        ref DestructionEventArgs args)
    {
        // If no selection has been made but our container is destroyed, populate the contents with one of the choices
        // at random so that the container still has everything someone breaking it open would expect it to have.
        // There's no reasonable way to provide a selection UI and option on destruction, since it's an instant event
        // that might not even have a player nearby, so random is the best they get.
        OnSelectionMade(ent, _random.Pick(ent.Comp.Selections));
    }

    private void OnSelectionMade(Entity<EntityTableContainerSelectionComponent> ent,
        ContainerSelectionEntry selection)
    {
        if (TerminatingOrDeleted(ent) || !Exists(ent))
            return;

        // This selection component has already delivered its goods, bail.
        if (ent.Comp.SelectionMade)
            return;

        if (!TryComp(ent, out ContainerManagerComponent? containerComp))
            return;

        var xform = Transform(ent);
        var coords = new EntityCoordinates(ent, Vector2.Zero);

        foreach (var (containerId, table) in selection.Containers)
        {
            SpawnTableInTarget(ent, containerComp, xform, containerId, table, coords);
        }

        // Close the UI, mark the selection as made, and let all the clients know so they stop updating the UI.
        _uiSystem.CloseUi(ent.Owner, ContainerSelectionUiKey.Key);
        ent.Comp.SelectionMade = true;
        Dirty(ent, ent.Comp);
    }

    private void SpawnTableInTarget(EntityUid target,
        ContainerManagerComponent containerComp,
        TransformComponent xform,
        string containerId,
        EntityTableSelector table,
        EntityCoordinates coords)
    {
        // Does our target container actually exist?
        if (!_containers.TryGetContainer(target, containerId, out var container, containerComp))
        {
            Log.Error(
                $"Entity {ToPrettyString(target)} with a {nameof(EntityTableContainerSelectionComponent)} is missing a container ({containerId}).");
            return;
        }

        // Get the contents we're filling with.
        var spawns = _entityTable.GetSpawns(table);
        foreach (var proto in spawns)
        {
            // Spawn the entity, try inserting it into the container, if we can't, log an error so someone knows
            // their entity prototype is overfull and drop it on the ground instead.
            var spawn = Spawn(proto, coords);
            if (!_containers.Insert(spawn, container, containerXform: xform))
            {
                var alreadyContained = container.ContainedEntities.Count > 0
                    ? string.Join("\n", container.ContainedEntities.Select(e => $"\t - {ToPrettyString(e)}"))
                    : "< empty >";
                Log.Error(
                    $"Entity {ToPrettyString(target)} with a {nameof(EntityTableContainerSelectionComponent)} failed to insert an entity: {ToPrettyString(spawn)}.\nCurrent contents:\n{alreadyContained}");
                _transform.AttachToGridOrMap(spawn);
                break;
            }
        }
    }
}
