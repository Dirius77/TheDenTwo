using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(EntitySystems.SharedLanguageSystem))]
public sealed partial class LanguageCommunicatorComponent : Component
{
    public const string ContainerId = "languages";

    [ViewVariables]
    public Container? Languages;

    [AutoNetworkedField]
    public EntityUid? CurrentLanguage;

    [AlwaysPushInheritance]
    [DataField("languages")]
    public Dictionary<ProtoId<LanguagePrototype>, (bool, ProtoId<LanguageFluencyPrototype>)> BaseLanguages = new();
}
