using Content.Server.Body.Systems;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._CD.Traits;

public sealed partial class SynthSystem : EntitySystem
{
    private static readonly ProtoId<TypingIndicatorPrototype> RobotTypingIndicator = "robot"; // Misfit - Type safety
    private static readonly ProtoId<ReagentPrototype> SynthBlood = "SynthBlood"; // Misfit - Type safety

    [Dependency] private BloodstreamSystem _bloodstream = default!; // TheDen - remove readonly
    [Dependency] private SharedTypingIndicatorSystem _typingIndicator = default!; // TheDen - Switch to set indicator prototype & remove readonly

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SynthComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, SynthComponent component, ComponentStartup args)
    {
        _typingIndicator.SetIndicatorPrototype(uid, RobotTypingIndicator); // TheDen - Switch to set indicator prototype

        // Give them synth blood. Ion storm notif is handled in that system
        _bloodstream.ChangeBloodReagent(uid, SynthBlood); // Misfit - Type safety
    }
}
