using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Synth;

public sealed partial class SynthSystem : EntitySystem
{
    public static readonly ProtoId<ReagentPrototype> SynthBlood = "SynthBlood";
    public static readonly ProtoId<TypingIndicatorPrototype> RobotTypingIndicator = "robot";

    [Dependency] private BloodstreamSystem _bloodstream = null!;
    [Dependency] private SharedTypingIndicatorSystem _typingIndicator = null!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SynthComponent> ent, ref MapInitEvent args)
    {
        // test fail
        if (!HasComp<BloodstreamComponent>(ent))
            return;

        var maxBlood = _bloodstream.GetBloodSolutionCapacity(ent.Owner);
        ent.Comp.SynthBlood.ScaleTo(maxBlood);

        _typingIndicator.SetIndicatorPrototype(ent, RobotTypingIndicator);

        // Give them synth blood. Ion storm notif is handled in that system
        _bloodstream.ChangeBloodReagents(ent.Owner, ent.Comp.SynthBlood);
    }
}
