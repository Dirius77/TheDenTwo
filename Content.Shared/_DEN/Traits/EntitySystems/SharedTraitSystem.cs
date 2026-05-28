using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._DEN.Traits.Components;
using Content.Shared._DEN.Traits.Prototypes;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

#pragma warning disable IDE1006 // Naming Styles
namespace Content.Shared._DEN.Traits.EntitySystems;
#pragma warning restore IDE1006 // Naming Styles

public abstract partial class SharedTraitSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private EntityQuery<TraitHolderComponent> _holderQuery;
    private EntityQuery<TraitComponent> _traitQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitHolderComponent, ComponentInit>(OnTraitHolderInit);
        SubscribeLocalEvent<TraitHolderComponent, ComponentShutdown>(OnTraitHolderShutdown);
        SubscribeLocalEvent<TraitHolderComponent, EntInsertedIntoContainerMessage>(OnTraitHolderEntityInserted);
        SubscribeLocalEvent<TraitHolderComponent, EntRemovedFromContainerMessage>(OnTraitHolderEntityRemoved);

        SubscribeLocalEvent<TraitComponent, ComponentShutdown>(OnTraitShutdown);

        _holderQuery = GetEntityQuery<TraitHolderComponent>();
        _traitQuery = GetEntityQuery<TraitComponent>();
    }

    private void OnTraitHolderInit(Entity<TraitHolderComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Traits = _container.EnsureContainer<Container>(ent, TraitHolderComponent.ContainerId);
    }

    private void OnTraitHolderShutdown(Entity<TraitHolderComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Traits is { } container)
            _container.ShutdownContainer(container);
    }

    private void OnTraitHolderEntityInserted(Entity<TraitHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_traitQuery.TryComp(args.Entity, out var traitComp)
            && traitComp.Holder != ent.Owner)
        {
            traitComp.Holder = ent.Owner;
            ActivateTrait((args.Entity, traitComp));
        }
    }

    private void OnTraitHolderEntityRemoved(Entity<TraitHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_traitQuery.TryComp(args.Entity, out var traitComp)
            && traitComp.Holder == ent.Owner)
            DeactivateTrait((args.Entity, traitComp));
    }

    private void OnTraitShutdown(Entity<TraitComponent> ent, ref ComponentShutdown args)
    {
        DeactivateTrait(ent);
    }

    private void ActivateTrait(Entity<TraitComponent> ent)
    {
        if (ent.Comp.Holder is null)
            return;

        foreach (var function in ent.Comp.TraitFunctions)
            function.OnTraitAdded(ent.Comp.Holder.Value, EntityManager);
    }

    private void DeactivateTrait(Entity<TraitComponent> ent)
    {
        if (ent.Comp.Holder is null)
            return;

        // We do this backwards to ensure traits are reversed in the correct order -
        // i.e. if earlier steps are setting up for later steps.
        foreach (var function in ent.Comp.TraitFunctions.Reverse())
            function.OnTraitRemoved(ent.Comp.Holder.Value, EntityManager);
    }

    private bool TryAddTraitEntity(EntityUid target, EntityUid traitEntity)
    {
        var holder = EnsureComp<TraitHolderComponent>(target);
        return holder.Traits is { } container
            && _container.Insert(traitEntity, container);
    }
}
