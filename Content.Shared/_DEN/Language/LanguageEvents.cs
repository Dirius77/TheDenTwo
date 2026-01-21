using Content.Shared._DEN.Language.Prototypes;
using Content.Shared.Speech;

namespace Content.Shared._DEN.Language;

/// <summary>
/// Obfuscates the given message using the provided language. ObfuscatedMessage will be filled in when the event resolves.
/// </summary>
/// <param name="language">The language to obfuscate with</param>
/// <param name="originalMessage">The original message.</param>
public sealed partial class ObfuscateLanguageEvent(LanguagePrototype language, string originalMessage) : HandledEntityEventArgs
{
    public LanguagePrototype Language = language;
    public string OriginalMessage = originalMessage;
    public string ObfuscatedMessage = "";
}

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
