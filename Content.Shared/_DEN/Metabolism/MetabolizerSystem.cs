using System.Linq;
using Content.Shared.Body;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Metabolism;

public sealed partial class MetabolizerSystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, AddTraitMetabolizerEvent>(_body.RelayEvent);
        SubscribeLocalEvent<BodyComponent, RemoveTraitMetabolizerEvent>(_body.RelayEvent);
    }

    /// <summary>
    ///     Adds metabolizers to metabolizer organs that pass a whitelist.
    /// </summary>
    /// <param name="ent">The metabolizer organ.</param>
    [SubscribeLocalEvent]
    private void OnAddTraitMetabolizerEvent(Entity<MetabolizerComponent> ent,
        ref BodyRelayedEvent<AddTraitMetabolizerEvent> args)
    {
        if (args.Args.Metabolizers.Count == 0
            || _whitelist.IsWhitelistFail(args.Args.OrganWhitelist, ent))
            return;

        var metabolizers = args.Args.Metabolizers;

        if (ent.Comp.MetabolizerTypes != null)
            metabolizers = metabolizers.Union(ent.Comp.MetabolizerTypes).ToHashSet();

        ent.Comp.MetabolizerTypes = metabolizers;
        Dirty(ent);
    }

    /// <summary>
    ///     Removes metabolizers from metabolizer organs that pass a whitelist.
    /// </summary>
    /// <param name="ent">The metabolizer organ.</param>
    [SubscribeLocalEvent]
    private void OnRemoveTraitMetabolizerEvent(Entity<MetabolizerComponent> ent,
        ref BodyRelayedEvent<RemoveTraitMetabolizerEvent> args)
    {
        if (args.Args.Metabolizers.Count == 0
            || ent.Comp.MetabolizerTypes == null
            || _whitelist.IsWhitelistFail(args.Args.OrganWhitelist, ent))
            return;

        // TODO: add smarter handling for this
        var metabolizers = ent.Comp.MetabolizerTypes
            .Except(args.Args.Metabolizers)
            .ToHashSet();

        ent.Comp.MetabolizerTypes = metabolizers;
        Dirty(ent);
    }
}

/// <summary>
///     An event that adds metabolizers to subscribed entities that pass the whitelist.
/// </summary>
/// <param name="Metabolizers">The metabolizers to add.</param>
/// <param name="OrganWhitelist">The whitelist for organs to add metabolizers to.</param>
[ByRefEvent]
public record struct AddTraitMetabolizerEvent(HashSet<ProtoId<MetabolizerTypePrototype>> Metabolizers,
    EntityWhitelist? OrganWhitelist = null);

/// <summary>
///     An event that removes metabolizers from subscribed entities that pass the whitelist.
/// </summary>
/// <param name="Metabolizers">The metabolizers to add.</param>
/// <param name="OrganWhitelist">The whitelist for organs to add metabolizers to.</param>
[ByRefEvent]
public record struct RemoveTraitMetabolizerEvent(HashSet<ProtoId<MetabolizerTypePrototype>> Metabolizers,
    EntityWhitelist? OrganWhitelist = null);
