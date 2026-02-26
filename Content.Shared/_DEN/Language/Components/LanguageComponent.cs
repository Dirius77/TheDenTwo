using Content.Shared._DEN.Language.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language.Components;

[RegisterComponent]
[Access(typeof(SharedLanguageSystem))]
public sealed partial class LanguageComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<LanguagePrototype> Language;

    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<LanguageFluencyPrototype> Fluency;

    // Maybe should be tied to fluency, but it could be useful for this to be asymmetric later.
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Speaks;

    // The entity currently holding this language. This will be null for the default language as it is shared by every entity.
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Holder;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> Children = new();
}
