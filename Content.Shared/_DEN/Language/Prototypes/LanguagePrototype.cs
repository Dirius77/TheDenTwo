using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._DEN.Language.Prototypes;

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
    public LanguageObfuscationEffect ObfuscationEffect { get; private set; }

    // What form of communication this language uses.
    [DataField(required: true)]
    public CommunicationType CommunicationType { get; private set; }

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<LanguagePrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}

/// <summary>
/// What form of communication this language uses. Used for determining things such as whether accents should be
/// applied.
/// </summary>
public enum CommunicationType
{
    Verbal, // Spoken out loud, applies accents and requires a free mouth.
    // TODO: Implement mental communication.
    Mental, // Spoken mentally, just requires the user to be conscious.
    Physical, // Spoken using body movements, requires free use of hands.
}
