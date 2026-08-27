using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Synth;

/// <summary>
/// Set players' blood to coolant
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SynthSystem))]
public sealed partial class SynthComponent : Component
{
    [DataField, AutoNetworkedField]
    public Solution SynthBlood = new([new ReagentQuantity(SynthSystem.SynthBlood, 600)]);
}
