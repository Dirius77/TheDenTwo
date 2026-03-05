using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Speech;

namespace Content.Server.SurveillanceCamera;

public sealed partial class SurveillanceCameraSpeakerSystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<SurveillanceCameraSpeakerComponent, SurveillanceCameraSpeechLanguageSendEvent>(
            OnSpeechLanguageSent);
    }

    private void OnSpeechLanguageSent(EntityUid uid,
        SurveillanceCameraSpeakerComponent component,
        SurveillanceCameraSpeechLanguageSendEvent args)
    {
        if (!component.SpeechEnabled)
            return;

        var time = _gameTiming.CurTime;
        var cd = TimeSpan.FromSeconds(component.SpeechSoundCooldown); // The docs say not to do this but I'm not here to fix the component.

        // I agree with the comment in the original file.
        // Maybe SpeechNoiseSystem just needs a "Play noise for message" function.
        if (time - component.LastSoundPlayed < cd
            && TryComp<SpeechComponent>(args.Speaker, out var speech))
        {
            var sound = _speechSound.GetSpeechSound((args.Speaker, speech),
                args.Message.Parts.LastOrDefault(part => part.Item1 == ChatPart.Dialog).Item2);

            _audioSystem.PlayPvs(sound, uid);

            component.LastSoundPlayed = time;
        }

        var nameEv = new TransformSpeakerNameEvent(args.Speaker, Name(args.Speaker));
        RaiseLocalEvent(args.Speaker, nameEv);

        var name = Loc.GetString("speech-name-relay",
            ("speaker", Name(uid)),
            ("originalName", nameEv.VoiceName));

        _chatSystem.SendEntityComplexSpeech(uid,
            args.Message,
            ChatSystem.SpeakWrapper,
            ChatChannel.Whisper,
            ChatTransmitRange.GhostRangeLimit,
            null,
            name,
            verbOverride: args.Verb,
            languageOverride: args.LanguageEnt);
    }
}
