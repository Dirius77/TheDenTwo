using Content.Shared._DEN.Language;
using Content.Shared.Chat;

namespace Content.Shared._DEN.Speech;

public sealed class ListenLanguageEvent : EntityEventArgs
{
    public readonly ComplexChatMessage Message;
    public readonly EntityUid Source;
    public readonly LanguagePrototype Language;
    public readonly string Verb;
    public readonly bool Whisper;

    public ListenLanguageEvent(ComplexChatMessage msg, EntityUid source, LanguagePrototype lang, string verb, bool whisper)
    {
        Message = msg;
        Source = source;
        Language = lang;
        Verb = verb;
        Whisper = whisper;
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
