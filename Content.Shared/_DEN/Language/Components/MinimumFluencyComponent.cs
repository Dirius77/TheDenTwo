using Content.Shared.Chat;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language.Components;

[RegisterComponent]
public sealed partial class MinimumFluencyComponent : Component
{
    [DataField("minimum", required: true)]
    public ProtoId<LanguageFluencyPrototype> MinimumFluency;

    [DataField]
    public Dictionary<ChatPart, List<LocId>>? Replacements;
}
