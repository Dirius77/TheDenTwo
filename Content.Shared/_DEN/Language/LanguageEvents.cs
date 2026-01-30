using Content.Shared._DEN.Language.Prototypes;

namespace Content.Shared._DEN.Language;

/// <summary>
/// Event sent targeting the entity trying to understand a particular statement in a language.
/// </summary>
/// <param name="sourceEntity"></param>
/// <param name="language"></param>
/// <param name="message"></param>
public sealed partial class DetermineUnderstandingEvent(
    EntityUid sourceEntity,
    LanguagePrototype language, string message) : HandledEntityEventArgs
{
    public EntityUid SourceEntity = sourceEntity;
    public LanguagePrototype Language = language;
    public string OriginalMessage = message;
    public bool Understands = false;
    public bool Hide = false; // Hide the message completely.
    public string? MessageOverride;
}
