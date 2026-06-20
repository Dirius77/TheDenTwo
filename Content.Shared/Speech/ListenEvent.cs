using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;

namespace Content.Shared.Speech;

// DEN Start: Switch to using ComplexChatMessage.
public sealed class ListenEvent : EntityEventArgs
{
    public readonly ComplexChatMessage Message;
    public readonly Entity<LanguageComponent> LanguageEnt;
    public readonly EntityUid Source;
    public readonly string Verb;
    public readonly ChatChannel Channel;

    public ListenEvent(ComplexChatMessage msg, EntityUid source, Entity<LanguageComponent> languageEnt, string verb, ChatChannel channel)
    {
        Message = msg;
        Source = source;
        LanguageEnt = languageEnt;
        Verb = verb;
        Channel = channel;
    }
}
// DEN End.

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;
    public readonly Entity<LanguageComponent> LanguageEnt; // DEN: Language

    public ListenAttemptEvent(EntityUid source, Entity<LanguageComponent> languageEnt) // DEN: Language
    {
        Source = source;
        LanguageEnt = languageEnt; // DEN: Language
    }
}
