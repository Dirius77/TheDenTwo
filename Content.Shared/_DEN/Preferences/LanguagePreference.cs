using Content.Shared._DEN.Language;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences;

[DataDefinition]
[Serializable, NetSerializable]
public partial struct LanguagePreference
{
    public ProtoId<LanguageFluencyPrototype> Fluency;
    public SpokenState Speaks;
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