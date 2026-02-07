using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language.Components;

[RegisterComponent]
[Access(typeof(EntitySystems.SharedLanguageSystem))]
public sealed partial class LanguageComponent : Component
{
    public ProtoId<LanguagePrototype> Language;
    public LanguageFluencyPrototype Fluency;

    // Maybe should be tied to fluency, but it could be useful for this to be asymmetric later.
    public bool Speaks;

    public EntityUid Holder;

    public List<EntityUid> Children;
}
