using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language.Components;

[RegisterComponent]
public sealed partial class TranslatorComponent : Component
{
    public static readonly float DefaultWattage = 0.4f; // ~30 minutes on a medium power cell.

    /// <summary>
    ///     The language this translator needs the user to be able to speak.
    /// </summary>
    [DataField("requires")]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<LanguagePrototype>? RequiredLanguage;

    /// <summary>
    ///     Languages granted by this translator, as well as their fluency and if they can be spoken.
    /// </summary>
    [DataField("grants")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<ProtoId<LanguagePrototype>, (bool, ProtoId<LanguageFluencyPrototype>)> GrantedLanguageProtos = new();

    /// <summary>
    ///     The amount of power that this translator drains, assuming it uses any. Defaults to `DefaultWattage` if it
    ///     does use power and this isn't set.
    /// </summary>
    [DataField]
    public float? Wattage;

    /// <summary>
    ///     Actual language entities currently from this translator.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<Entity<LanguageComponent>> GrantedLanguages = new();

    public EntityUid? CurrentlyGrantingTo;
}
