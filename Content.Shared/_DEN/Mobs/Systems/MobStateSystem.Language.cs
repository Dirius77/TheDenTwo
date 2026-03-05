using Content.Shared._DEN.Speech;
using Content.Shared.Damage.ForceSay;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Mobs.Systems;

public partial class MobStateSystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<MobStateComponent, SpeakLanguageAttemptEvent>(OnSpeakLanguageAttempt);
    }

    private void OnSpeakLanguageAttempt(Entity<MobStateComponent> uid, ref SpeakLanguageAttemptEvent evt)
    {
        // TODO: Decide if UnconsciousLanguages should be able to be spoken while critical.

        if (HasComp<AllowNextCritSpeechComponent>(uid))
        {
            RemCompDeferred<AllowNextCritSpeechComponent>(uid);
            return;
        }

        CheckAct(uid, uid.Comp, evt);
    }
}
