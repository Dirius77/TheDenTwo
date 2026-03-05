using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;

namespace Content.Shared._DEN.Speech;

public sealed class ListenLanguageEvent : EntityEventArgs
{
    public readonly ComplexChatMessage Message;
    public readonly Entity<LanguageComponent?> LanguageEnt;
    public readonly EntityUid Source;
    public readonly string Verb;
    public readonly ChatChannel Channel;

    public ListenLanguageEvent(ComplexChatMessage msg, EntityUid source, Entity<LanguageComponent?> languageEnt, string verb, ChatChannel channel)
    {
        Message = msg;
        Source = source;
        LanguageEnt = languageEnt;
        Verb = verb;
        Channel = channel;
    }
}

public sealed class ListenLanguageAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;
    public readonly Entity<LanguageComponent?> LanguageEnt;

    public ListenLanguageAttemptEvent(EntityUid source, Entity<LanguageComponent?> languageEnt)
    {
        Source = source;
        LanguageEnt = languageEnt;
    }
}
