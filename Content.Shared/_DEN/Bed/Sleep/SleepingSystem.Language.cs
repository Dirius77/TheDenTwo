using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Speech;
using Content.Shared.Damage.ForceSay;

namespace Content.Shared.Bed.Sleep;

public sealed partial class SleepingSystem
{
    [Dependency] private EntityQuery<UnconsciousLanguageComponent> _unconsciousLanguageQuery = default!;

    public void InitializeLanguage()
    {
        SubscribeLocalEvent<SleepingComponent, SpeakLanguageAttemptEvent>(OnSpeakLanguageAttempt);
    }

    private void OnSpeakLanguageAttempt(Entity<SleepingComponent> ent, ref SpeakLanguageAttemptEvent args)
    {
        if (_unconsciousLanguageQuery.HasComp(args.LanguageEnt))
            return;

        if (HasComp<AllowNextCritSpeechComponent>(ent))
        {
            RemCompDeferred<AllowNextCritSpeechComponent>(ent);
            return;
        }

        args.Cancel();
    }
}
