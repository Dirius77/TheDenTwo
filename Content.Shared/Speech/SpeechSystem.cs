namespace Content.Shared.Speech
{
    public sealed partial class SpeechSystem : EntitySystem // DEN: Make partial
    {
        public override void Initialize()
        {
            base.Initialize();

            //SubscribeLocalEvent<SpeakAttemptEvent>(OnSpeakAttempt); // DEN: Languages, see SpeakLanguageAttemptEvent
            InitializeLanguage(); // DEN: Languages
        }

        public void SetSpeech(EntityUid uid, bool value, SpeechComponent? component = null)
        {
            if (value && !Resolve(uid, ref component))
                return;

            component = EnsureComp<SpeechComponent>(uid);

            if (component.Enabled == value)
                return;

            component.Enabled = value;

            Dirty(uid, component);
        }

        [Obsolete("Use OnSpeakLanguageAttempt instead.", true)] // DEN: Languages
        private void OnSpeakAttempt(SpeakAttemptEvent args)
        {
            if (!TryComp(args.Uid, out SpeechComponent? speech) || !speech.Enabled)
                args.Cancel();
        }
    }
}
