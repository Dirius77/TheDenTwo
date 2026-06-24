using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;

namespace Content.Shared.Speech
{
    public sealed class SpeakAttemptEvent : CancellableEntityEventArgs, ISpokenLanguageRelayEvent // DEN: Languages
    {
        public SpeakAttemptEvent(EntityUid uid, Entity<LanguageComponent> languageEnt, ChatChannel? channel) // DEN: Languages
        {
            Uid = uid;
            LanguageEnt = languageEnt; // DEN: Languages
            Channel = channel; // DEN: Languages
        }

        public EntityUid Uid { get; }
        public Entity<LanguageComponent> LanguageEnt; // DEN: languages
        public ChatChannel? Channel; // DEN: Languages
    }
}
