using System.Linq;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Speech;

namespace Content.Server.Speech;

public sealed partial class SpeechSoundSystem
{
    private EntityQuery<AudibleComponent> _audibleQuery;

    private void InitializeLanguage()
    {
        _audibleQuery = GetEntityQuery<AudibleComponent>();

        SubscribeLocalEvent<SpeechComponent, EntitySpokeLanguageEvent>(OnEntitySpokeLanguage);
    }

    private void OnEntitySpokeLanguage(EntityUid uid, SpeechComponent component, EntitySpokeLanguageEvent args)
    {
        if (component.SpeechSounds == null)
            return;

        if (!_audibleQuery.HasComponent(args.LanguageEnt))
            return;

        var currentTime = _gameTiming.CurTime;
        var cooldown = TimeSpan.FromSeconds(component.SoundCooldownTime);

        if (currentTime - component.LastTimeSoundPlayed < cooldown)
            return;

        var lastDialog = args.Message.Parts.LastOrDefault(part => part.Item1 == ChatPart.Dialog).Item2;

        // The "Speech" didn't actually contain any dialog.
        if (lastDialog == null)
            return;

        var sound = GetSpeechSound((uid, component), lastDialog);
        component.LastTimeSoundPlayed = currentTime;
        _audio.PlayPvs(sound, uid);
    }
}
