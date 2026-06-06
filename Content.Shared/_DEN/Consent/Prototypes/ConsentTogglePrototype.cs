using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Consent.Prototypes;

/// <summary>
/// This is a prototype for declaring consent toggles.
/// </summary>
[Prototype("consent")]
public sealed partial class ConsentTogglePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public bool DefaultValue { get; set; }
}
