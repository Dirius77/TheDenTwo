using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language;

[Prototype]
public sealed partial class LanguagePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public LocId Abbreviation => Name + "-abbreviation";

    [ViewVariables(VVAccess.ReadOnly)]
    public LocId Description => Name + "-description";

    /// <summary>
    ///     Override the base speaking verb for the language.
    /// </summary>
    [DataField]
    public ProtoId<SpeechVerbPrototype>? SpeechVerb;

    /// <summary>
    ///     Overrides for the speaking chat suffixes, for example '?' or '!!' makings things asks or yells.
    /// </summary>
    // Languages replace a lot of the functionality in SpeechComponent, and maybe should completely bypass it
    // if this code was going to end up upstream.
    [DataField]
    public Dictionary<string, ProtoId<SpeechVerbPrototype>>? SuffixSpeechVerbs;

    /// <summary>
    ///     The scrambler to use for this language.
    /// </summary>
    [DataField(required: true)]
    public ILanguageScrambler Scrambler { get; private set; } = default!;

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
    public Dictionary<ProtoId<LanguagePrototype>, ProtoId<LanguageFluencyPrototype>> RelatedLanguages = new();

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}
