using Content.Shared._DEN.Language;

namespace Content.Shared.Speech
{
    public sealed class SpeakAttemptEvent : CancellableEntityEventArgs, ISpokenLanguageRelayEvent // DEN: Languages
    {
        public SpeakAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
