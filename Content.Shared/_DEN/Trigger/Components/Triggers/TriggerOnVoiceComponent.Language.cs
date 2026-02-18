using Content.Shared._DEN.Language;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Triggers;

public sealed partial class TriggerOnVoiceComponent
{
    [DataField, AutoNetworkedField]
    public ProtoId<LanguagePrototype>? KeyLanguage;

    [DataField, AutoNetworkedField]
    public ProtoId<LanguagePrototype>? DefaultKeyLanguage;
}
