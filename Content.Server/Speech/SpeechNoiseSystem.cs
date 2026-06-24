using System.Linq;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Speech
{
    public sealed partial class SpeechSoundSystem : EntitySystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;
        [Dependency] private IPrototypeManager _protoManager = default!;
        [Dependency] private IRobustRandom _random = default!;
        [Dependency] private SharedAudioSystem _audio = default!;
        [Dependency] private EntityQuery<AudibleComponent> _audibleQuery = default!; // DEN: Languages

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpeechComponent, EntitySpokeEvent>(OnEntitySpoke);
        }

        public SoundSpecifier? GetSpeechSound(Entity<SpeechComponent> ent, string message)
        {
            // MACRO Start: SpeechSounds
            //if (ent.Comp.SpeechSounds == null)
            //    return null;
            var protoId = ent.Comp.SpeechSounds;

            // raise event for voice-changing equipment
            var voiceEv = new TransformSpeakerVoiceEvent(ent);
            RaiseLocalEvent(ent, voiceEv);
            protoId = voiceEv.SpeechSounds ?? protoId;

            if (protoId == null)
                return null;
            // MACRO End: SpeechSounds

            // Play speech sound
            SoundSpecifier? contextSound;
            var prototype = _protoManager.Index<SpeechSoundsPrototype>(protoId); // MACRO: SpeechSounds, change to protoId

            // Different sounds for ask/exclaim based on last character
            contextSound = message[^1] switch
            {
                '?' => prototype.AskSound,
                '!' => prototype.ExclaimSound,
                _ => prototype.SaySound
            };

            // Use exclaim sound if most characters are uppercase.
            int uppercaseCount = 0;
            for (int i = 0; i < message.Length; i++)
            {
                if (char.IsUpper(message[i]))
                    uppercaseCount++;
            }
            if (uppercaseCount > (message.Length / 2))
            {
                contextSound = prototype.ExclaimSound;
            }

            var scale = (float) _random.NextGaussian(1, prototype.Variation);
            contextSound.Params = ent.Comp.AudioParams.WithPitchScale(scale);
            return contextSound;
        }

        private void OnEntitySpoke(EntityUid uid, SpeechComponent component, EntitySpokeEvent args)
        {
            if (component.SpeechSounds == null)
                return;

            // DEN Only audible languages make sounds
            if (!_audibleQuery.HasComponent(args.LanguageEnt))
                return;

            var currentTime = _gameTiming.CurTime;
            var cooldown = TimeSpan.FromSeconds(component.SoundCooldownTime);

            // Ensure more than the cooldown time has passed since last speaking
            if (currentTime - component.LastTimeSoundPlayed < cooldown)
                return;

            // DEN Start: Use complex speech for sounds.
            var lastDialog = args.Message.Parts.LastOrDefault(part => part.Item1 == ChatPart.Dialog).Item2;

            // The "Speech" didn't actually contain any dialog.
            if (lastDialog == null)
                return;

            var sound = GetSpeechSound((uid, component), lastDialog);
            // DEN End
            component.LastTimeSoundPlayed = currentTime;
            _audio.PlayPvs(sound, uid);
        }
    }
}
