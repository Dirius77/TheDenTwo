using Content.Shared._DEN.Language.Prototypes;
using Content.Shared._DEN.Language.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language.Components;

/// <summary>
/// A component that is used to make an entity speak and understand specific languages.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(LanguageSystem))]
public sealed partial class LanguageCommunicatorComponent : Component
{
    /// <summary>
    /// Which language this entity currently has selected.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public ProtoId<LanguagePrototype>? CurrentLanguage { get; set; }

    /// <summary>
    /// The languages that this entity is capable of speaking in.
    /// </summary>
    [DataField("spoken")]
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public HashSet<ProtoId<LanguagePrototype>> SpokenLanguages { get; set; } = new();

    /// <summary>
    /// The languages that this entity is capable of understanding.
    /// </summary>
    [DataField("understood")]
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public HashSet<ProtoId<LanguagePrototype>> UnderstoodLanguages { get; set; } = new();
}
