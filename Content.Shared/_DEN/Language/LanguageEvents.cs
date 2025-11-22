using Content.Shared._DEN.Language.Prototypes;

namespace Content.Shared._DEN.Language;

public sealed class ObfuscateLanguageEvent: HandledEntityEventArgs
{
    public LanguagePrototype Language;
    public string OriginalMessage;
    public string? ObfuscatedMessage;

    public ObfuscateLanguageEvent(LanguagePrototype language, string originalMessage)
    {
        Language = language;
        OriginalMessage = originalMessage;
    }
}

public sealed class WrapMessageEvent : HandledEntityEventArgs
{
    public LanguagePrototype Language;
    public string OriginalMessage;
    public string? WrappedMessage;
}
