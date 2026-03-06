using Content.Shared._DEN.Language;

namespace Content.Shared.Speech
{
    [Obsolete("Use SpeakLanguageAttemptEvent instead", true)] // DEN: languages
    public sealed class SpeakAttemptEvent : CancellableEntityEventArgs
    {
        public SpeakAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
