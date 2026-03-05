using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Speech;
using Content.Shared.Abilities.Mime;
using Content.Shared.Puppet;
using Content.Shared.Speech.Muting;

namespace Content.Server.Speech.Muting;

public sealed partial class MutingSystem
{
    private EntityQuery<AudibleComponent> _audibleQuery;

    public void InitializeLanguage()
    {
        _audibleQuery = GetEntityQuery<AudibleComponent>();

        SubscribeLocalEvent<MutedComponent, SpeakLanguageAttemptEvent>(OnSpeakLanguageAttempt);
    }

    private void OnSpeakLanguageAttempt(Entity<MutedComponent> ent, ref SpeakLanguageAttemptEvent args)
    {
        // Non-audible languages are not impacted by being unable to make sound.
        if (!_audibleQuery.HasComp(args.LanguageEnt))
            return;

        if (HasComp<MimePowersComponent>(ent))
            _popupSystem.PopupEntity(Loc.GetString("mime-cant-speak"), ent, ent);
        else if (HasComp<VentriloquistPuppetComponent>(ent))
            _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-cant-speak"), ent, ent);
        else
            _popupSystem.PopupEntity(Loc.GetString("speech-muted"), ent, ent);

        args.Cancel();
    }
}
