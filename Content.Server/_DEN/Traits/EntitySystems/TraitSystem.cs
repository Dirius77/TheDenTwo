using Content.Shared._DEN.Requirements.Managers;
using Content.Shared._DEN.Traits.EntitySystems;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

#pragma warning disable IDE1006 // Naming Styles
namespace Content.Server._DEN.Traits.EntitySystems;
#pragma warning restore IDE1006 // Naming Styles

public sealed partial class TraitSystem : SharedTraitSystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IPlayerRequirementManager _requirements = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.JobId == null
            || !_prototypeManager.Resolve<JobPrototype>(args.JobId, out var protoJob)
            || !protoJob.ApplyTraits)
            return;

        var mob = args.Mob;

        foreach (var traitId in args.Profile.EntityTraitPreferences)
        {
            if (!_prototypeManager.TryIndex(traitId, out var trait))
            {
                Log.Error($"No trait found with ID {traitId}!");
                continue;
            }

            if (!trait.Selectable)
            {
                Log.Error($"Tried to spawn unselectable trait {traitId} on {ToPrettyString(mob)}!");
                continue;
            }

            var context = _requirements.GetPlayerContext(args.Player);
            context.Profile = args.Profile;
            if (!SharedPlayerRequirementManager.CheckRequirements(context, trait.Requirements))
            {
                Log.Error($"Tried to spawn trait {traitId} on {ToPrettyString(mob)}, but we failed the requirements to do so!");
                continue;
            }

            TryAddTrait(mob, traitId, out _);
        }
    }
}
