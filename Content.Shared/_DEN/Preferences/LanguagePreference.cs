using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences;

[DataDefinition]
[Serializable, NetSerializable]
public partial struct LanguagePreference
{
    [DataField]
    public ProtoId<LanguageFluencyPrototype> Fluency = SharedLanguageSystem.MinimumFluency;
    
    [DataField]
    public SpokenState Speaks = SpokenState.None;
    
    [DataField]
    public bool Primary;

    public LanguagePreference(ProtoId<LanguageFluencyPrototype> fluency, SpokenState speaks, bool primary)
    {
        Fluency = fluency;
        Speaks = speaks;
        Primary = primary;
    }
}

public enum SpokenState : byte
{
    None,
    Speaks,
    Translator
}