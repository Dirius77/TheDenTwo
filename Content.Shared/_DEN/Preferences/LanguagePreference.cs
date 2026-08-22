using Content.Shared._DEN.Language;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences;

[DataDefinition]
[Serializable, NetSerializable]
public partial struct LanguagePreference
{
    public ProtoId<LanguageFluencyPrototype> Fluency;
    public bool Speaks;
    public bool Primary;

    public LanguagePreference(ProtoId<LanguageFluencyPrototype> fluency, bool speaks, bool primary)
    {
        Fluency = fluency;
        Speaks = speaks;
        Primary = primary;
    }
}