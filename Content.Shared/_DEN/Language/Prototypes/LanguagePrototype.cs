using Content.Shared.Chat;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._DEN.Language;

[Prototype]
public sealed partial class LanguagePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public LocId Abbreviation => Name + "-abbreviation";

    [ViewVariables(VVAccess.ReadOnly)]
    public LocId Description => Name + "-description";

    public string LocalizedName => Loc.GetString(Name);
    public string LocalizedAbbreviation => Loc.GetString(Abbreviation);
    public string LocalizedDescription => Loc.GetString(Description);

    /// <summary>
    ///     Speech verb overrides per channel, with optional suffix verbs.
    /// </summary>
    [DataField]
    public Dictionary<ChatChannel, LanguageSpeechVerbs>? SpeechVerbs;

    /// <summary>
    ///     The font to use for this language.
    /// </summary>
    [DataField]
    public string FontId = "Default";

    /// <summary>
    ///     The font size to use for this language.
    /// </summary>
    [DataField]
    public int FontSize = 12;

    /// <summary>
    ///     The font color to use for this language.
    /// </summary>
    [DataField]
    public Color FontColor = Color.White;

    /// <summary>
    ///     Whether to display this language in chat.
    /// </summary>
    [DataField]
    public bool DisplayInChat = false;

    /// <summary>
    ///     How familiar with this language someone must be to recognize the language name in chat.
    ///     This does nothing unless DisplayInChat is true.
    /// </summary>
    [DataField]
    public ProtoId<LanguageFluencyPrototype> UnderstandingForDisplay = "Unfamiliar";

    [DataField]
    public Dictionary<ChatChannel, ProtoId<LanguageWrapperPrototype>>? WrapperOverrides;

    /// <summary>
    ///     Languages that are related to this language. If a speaker is completely Fluent in this language, then
    ///     they will also be able to understand the related languages in the specified amount.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<LanguagePrototype>, ProtoId<LanguageFluencyPrototype>> RelatedLanguages = new();

    /// <summary>
    ///     Other components to add to the language entity. These are used to add language specific effects
    ///     such as being spoken, signed, telepathic, or other such behavior.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry? LanguageComponents;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<LanguagePrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class LanguageSpeechVerbs
{
    [DataField]
    public ProtoId<SpeechVerbPrototype>? DefaultVerb;

    [DataField]
    public Dictionary<LocId, ProtoId<SpeechVerbPrototype>>? SuffixSpeechVerbs;
}
