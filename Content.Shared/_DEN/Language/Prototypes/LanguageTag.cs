using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language.Prototypes;

/// <summary>
/// Tagging system for languages, includes stuff like: Verbal, Mental, Hivemind, etc.
/// </summary>
[Prototype]
public sealed class LanguageTagPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name", required: true)]
    private LocId Name { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);
}
