using Content.Shared._DEN.Speech;

namespace Content.Shared.Speech;

public sealed partial class SpeechSystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<SpeakLanguageAttemptEvent>(OnSpeakLanguageAttempt);
    }

    private void OnSpeakLanguageAttempt(SpeakLanguageAttemptEvent evt)
    {
        if (!TryComp(evt.Uid, out SpeechComponent? speech) || !speech.Enabled)
            evt.Cancel();
    }
}
