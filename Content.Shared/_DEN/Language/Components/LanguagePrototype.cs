using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._DEN.Language.Components;

[Prototype]
[DataDefinition]
public sealed partial class LanguagePrototype : IPrototype, IInheritingPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    private LocId Name { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    // A shortened form of the Language for display in chat.
    // TODO: Add this to the YML checker to make sure it exists.
    [ViewVariables(VVAccess.ReadOnly)]
    public string Abbreviation => Loc.GetString(Name + "-abbreviation");

    [DataField]
    public LocId Description { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedDescription => Loc.GetString(Description);

    // Replace the normal speaking verbs when using this language, useful for things like Sign Language.
    [DataField]
    public ProtoId<SpeechVerbPrototype>? VerbOverride { get; private set; } = null!;

    [DataField(required: true)]
    public LanguageEffect Effect { get; private set; }

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<LanguagePrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}
