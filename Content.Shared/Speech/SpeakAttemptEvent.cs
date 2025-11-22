using Content.Shared._DEN.Language.Prototypes;

namespace Content.Shared.Speech
{
    public sealed class SpeakAttemptEvent : CancellableEntityEventArgs
    {
        // DEN: Add language to SpeakAttemptEvent
        public SpeakAttemptEvent(EntityUid uid, LanguagePrototype language)
        {
            Uid = uid;
            Language = language;
        }

        public EntityUid Uid { get; }
        public LanguagePrototype Language { get; } // DEN
    }
}
