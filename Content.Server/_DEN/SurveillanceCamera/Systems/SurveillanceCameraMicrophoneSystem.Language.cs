using Content.Shared._DEN.Language;
using Content.Shared._DEN.Speech;
using Content.Shared.Chat;
using Content.Shared.SurveillanceCamera.Components;

namespace Content.Server.SurveillanceCamera;

public sealed partial class SurveillanceCameraMicrophoneSystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ListenLanguageEvent>(RelayEntityLanguageMessage);
        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ListenLanguageAttemptEvent>(CanListenLanguage);
    }

    private void CanListenLanguage(EntityUid uid,
        SurveillanceCameraMicrophoneComponent microphone,
        ListenLanguageAttemptEvent args)
    {
        if (_whitelistSystem.IsWhitelistPass(microphone.Blacklist, args.Source))
            args.Cancel();
    }

    private void RelayEntityLanguageMessage(EntityUid uid,
        SurveillanceCameraMicrophoneComponent component,
        ListenLanguageEvent args)
    {
        if (!TryComp(uid, out SurveillanceCameraComponent? camera))
            return;

        var ev = new SurveillanceCameraSpeechLanguageSendEvent(args.Source, args.Message, args.Language, args.Verb);
    }
}

public sealed class SurveillanceCameraSpeechLanguageSendEvent : EntityEventArgs
{
    public EntityUid Speaker { get; }
    public ComplexChatMessage Message { get; }
    public LanguagePrototype Language { get; }
    public string Verb { get; }

    public SurveillanceCameraSpeechLanguageSendEvent(EntityUid speaker,
        ComplexChatMessage message,
        LanguagePrototype language,
        string verb)
    {
        Speaker = speaker;
        Message = message;
        Language = language;
        Verb = verb;
    }
}
