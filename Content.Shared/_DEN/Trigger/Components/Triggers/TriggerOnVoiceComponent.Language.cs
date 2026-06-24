using Content.Shared._DEN.Language;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Triggers;

public sealed partial class TriggerOnVoiceComponent
{
    /// <summary>
    /// The language this component is currently keyed in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<LanguagePrototype>? KeyLanguage;

    /// <summary>
    /// The default language this component is keyed in. Used when you want to make a prototype that has a pre-recorded phrase.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<LanguagePrototype>? DefaultKeyLanguage;
}
