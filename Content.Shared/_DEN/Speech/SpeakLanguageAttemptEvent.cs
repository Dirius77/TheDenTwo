using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;

namespace Content.Shared._DEN.Speech;

public sealed class SpeakLanguageAttemptEvent(EntityUid uid, Entity<LanguageComponent> languageEnt, ChatChannel? channel)
    : CancellableEntityEventArgs, ISpokenLanguageRelayEvent
{
    public EntityUid Uid = uid;
    public Entity<LanguageComponent> LanguageEnt = languageEnt;
    public ChatChannel? Channel = channel;
}
