using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Speech;
using Content.Shared.Chat;
using Content.Shared.SurveillanceCamera.Components;

namespace Content.Server.SurveillanceCamera.Systems;

public sealed partial class SurveillanceCameraMicrophoneSystem
{
    private EntityQuery<AudibleComponent> _audibleQuery;
    private EntityQuery<LineOfSightLanguageComponent> _losQuery;

    private void InitializeLanguage()
    {
        _audibleQuery = GetEntityQuery<AudibleComponent>();
        _losQuery = GetEntityQuery<LineOfSightLanguageComponent>();

        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ListenLanguageEvent>(RelayEntityLanguageMessage);
        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ListenLanguageAttemptEvent>(CanListenLanguage);
    }

    private void CanListenLanguage(EntityUid uid,
        SurveillanceCameraMicrophoneComponent microphone,
        ListenLanguageAttemptEvent args)
    {
        if (_whitelistSystem.IsWhitelistPass(microphone.Blacklist, args.Source)
            || !_audibleQuery.HasComponent(args.LanguageEnt)
            || !_losQuery.HasComponent(args.LanguageEnt))
            args.Cancel();
    }

    private void RelayEntityLanguageMessage(EntityUid uid,
        SurveillanceCameraMicrophoneComponent component,
        ListenLanguageEvent args)
    {
        if (!TryComp(uid, out SurveillanceCameraComponent? camera))
            return;

        var ev = new SurveillanceCameraSpeechLanguageSendEvent(args.Source, args.LanguageEnt, args.Message, args.Verb);
    }
}

public sealed class SurveillanceCameraSpeechLanguageSendEvent : EntityEventArgs
{
    public EntityUid Speaker { get; }
    public Entity<LanguageComponent> LanguageEnt { get; }
    public ComplexChatMessage Message { get; }
    public string Verb { get; }

    public SurveillanceCameraSpeechLanguageSendEvent(EntityUid speaker,
        Entity<LanguageComponent> languageEnt,
        ComplexChatMessage message,
        string verb)
    {
        Speaker = speaker;
        Message = message;
        LanguageEnt = languageEnt;
        Verb = verb;
    }
}
