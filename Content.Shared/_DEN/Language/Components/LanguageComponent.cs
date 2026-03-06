using Content.Shared._DEN.Language.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedLanguageSystem))]
public sealed partial class LanguageComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<LanguagePrototype> Language;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<LanguageFluencyPrototype> Fluency;

    // Maybe should be tied to fluency, but it could be useful for this to be asymmetric later.
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Speaks;

    // The entity currently holding this language.
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Holder;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> Children = new();
}
