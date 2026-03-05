using Content.Server.Chat.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Speech;
using Content.Shared.Chat;
using Content.Shared.Radio.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioDeviceSystem
{
    private EntityQuery<RadioTransmittableComponent> _radioLang;

    private void InitializeLanguage()
    {
        _radioLang = GetEntityQuery<RadioTransmittableComponent>();

        SubscribeLocalEvent<RadioMicrophoneComponent, ListenLanguageEvent>(OnListenLanguage);
        SubscribeLocalEvent<RadioMicrophoneComponent, ListenLanguageAttemptEvent>(OnAttemptListenLanguage);

        SubscribeLocalEvent<RadioSpeakerComponent, RadioReceiveLanguageEvent>(OnReceiveLanguageRadio);
    }

    private void OnListenLanguage(EntityUid uid, RadioMicrophoneComponent component, ListenLanguageEvent args)
    {
        if (HasComp<RadioSpeakerComponent>(args.Source))
            return;

        var channel = _protoMan.Index(component.BroadcastChannel);
        if (_recentlySent.Add((args.Message.OriginalMessage, args.Source, channel)))
            _radio.SendLanguageRadioMessage(args.Source, args.LanguageEnt, args.Message, channel, uid);
    }

    private void OnAttemptListenLanguage(EntityUid uid,
        RadioMicrophoneComponent component,
        ListenLanguageAttemptEvent args)
    {
        if (component.PowerRequired && !this.IsPowered(uid, EntityManager)
            || component.UnobstructedRequired && !_interaction.InRangeUnobstructed(args.Source, uid, 0)
            || !_radioLang.HasComp(args.LanguageEnt))
        {
            args.Cancel();
        }
    }

    private void OnReceiveLanguageRadio(EntityUid uid,
        RadioSpeakerComponent component,
        ref RadioReceiveLanguageEvent args)
    {
        if (uid == args.RadioSource)
            return;

        var nameEv = new TransformSpeakerNameEvent(args.MessageSource, Name(args.MessageSource));
        RaiseLocalEvent(args.MessageSource, nameEv);

        var name = Loc.GetString("speech-name-relay",
            ("speaker", Name(uid)),
            ("originalName", nameEv.VoiceName));

        _chat.SendEntityComplexSpeech(uid,
            args.Message,
            ChatSystem.WhisperWrapper,
            ChatTransmitRange.GhostRangeLimit,
            ChatChannel.Whisper,
            null,
            name,
            verbOverride: args.Verb,
            languageOverride: args.LanguageEnt);
    }
}
