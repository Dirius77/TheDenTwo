using Content.Shared._DEN.Language;
using Content.Shared.Chat;

namespace Content.Shared._DEN.Speech;

public sealed class ListenLanguageEvent : EntityEventArgs
{
    public readonly SharedChatSystem.ComplexChatMessage Message;
    public readonly EntityUid Source;
    public readonly LanguagePrototype Language;

    public ListenLanguageEvent(SharedChatSystem.ComplexChatMessage msg, EntityUid source, LanguagePrototype lang)
    {
        Message = msg;
        Source = source;
        Language = lang;
    }
}

public sealed class ListenLanguageAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;
    public readonly LanguagePrototype Language;

    public ListenLanguageAttemptEvent(EntityUid source, LanguagePrototype lang)
    {
        Source = source;
        Language = lang;
    }
}
