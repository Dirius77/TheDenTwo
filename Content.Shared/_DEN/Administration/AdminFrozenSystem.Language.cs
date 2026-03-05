using Content.Shared._DEN.Speech;

namespace Content.Shared.Administration;

public sealed partial class AdminFrozenSystem
{
    public void InitializeLanguage()
    {
        SubscribeLocalEvent<AdminFrozenComponent, SpeakLanguageAttemptEvent>(OnSpeakLanguageAttempt);
    }

    public void OnSpeakLanguageAttempt(Entity<AdminFrozenComponent> ent, ref SpeakLanguageAttemptEvent args)
    {
        if (!ent.Comp.Muted)
            return;

        args.Cancel();
    }
}
