using Content.Shared.Atmos.Components;
using Content.Shared.Clothing.Components;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    private void InitializeModsuits()
    {
        SubscribeLocalEvent<BreathToolComponent, ComponentInit>(OnBreathToolInit);
    }

    private void OnBreathToolInit(Entity<BreathToolComponent> entity, ref ComponentInit evt)
    {
        // The comment in the mask system is so right there needs to be a better way to get something's wearer.
        if (TryComp<ClothingComponent>(entity, out var clothing)
            && clothing.InSlotFlag is { } slotFlag
            && entity.Comp.AllowedSlots.HasFlag(slotFlag))
        {
            var wearer = Transform(entity).ParentUid;
            if (_internalsQuery.TryComp(wearer, out var internals))
                _internals.ConnectBreathTool((wearer, internals), entity);
        }
    }
}