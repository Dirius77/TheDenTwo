using Content.Shared._DEN.Requirements.PlayerRequirements;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language;

[Prototype]
[DataDefinition]
public sealed partial class LanguageEntryPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("language", required: true)] public ProtoId<LanguagePrototype> LanguageProto;

    /// <summary>
    /// The requirements to be able to select this language, if any.
    /// </summary>
    [DataField] public List<IPlayerRequirement> Requirements = new();

    /// <summary>
    /// Additional requirements beyond those in <see cref="Requirements"/> that must be met to be able to select the
    /// language for speaking. If Requirements is met but not SpeakingRequirements, then the language can still be
    /// selected as understood, just not spoken. IE: A borg can understand sign language, but lacks the hands to speak
    /// it.
    /// </summary>
    [DataField] public List<IPlayerRequirement> SpeakingRequirements = new();

    /// <summary>
    /// Influences the order languages are displayed in the selection menu. Higher numbers are displayed first, ties are
    /// sorted alphabetically.
    /// </summary>
    [DataField] public int Priority;

    /// <summary>
    /// Determines if this language can be translated by a handheld translator. Prevents selecting the "Translator" option
    /// at character creation if this is false.
    /// </summary>
    [DataField] public bool CanHaveTranslator = true;
}