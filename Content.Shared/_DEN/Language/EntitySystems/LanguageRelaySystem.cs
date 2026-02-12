using Content.Shared._DEN.Language.Components;

namespace Content.Shared._DEN.Language.EntitySystems;

public sealed partial class LanguageRelaySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<LanguageCommunicatorComponent, AttemptUnderstandingEvent>(RelaySpokenLanguageEvent);
    }

    public void RelayKnownLanguagesEvent<T>(EntityUid uid, LanguageCommunicatorComponent comp, T args)
        where T : IKnownLanguagesRelayEvent
    {
        RelayKnownEvent((uid, comp), ref args);
    }

    public void RelaySpokenLanguageEvent<T>(EntityUid uid, LanguageCommunicatorComponent comp, T args)
        where T : ISpokenLanguageRelayEvent
    {
        Log.Debug("Relaying Spoken language Event.");
        RelaySpokenEvent((uid, comp), ref args);
    }

    public void RelaySpokenEvent<T>(Entity<LanguageCommunicatorComponent> ent, ref T args)
        where T : ISpokenLanguageRelayEvent
    {
        var ev = new LanguageRelayedEvent<T>(ent, args);
        if (ent.Comp.CurrentLanguage != null)
        {
            RaiseLocalEvent(ent.Comp.CurrentLanguage.Value, ev);
        }
        args = ev.Args;
    }

    public void RelayKnownEvent<T>(Entity<LanguageCommunicatorComponent> ent, ref T args) where T : IKnownLanguagesRelayEvent
    {
        var ev = new LanguageRelayedEvent<T>(ent, args);
        if (ent.Comp.Languages != null)
        {
            foreach (var langEnt in ent.Comp.Languages.ContainedEntities)
            {
                RaiseLocalEvent(langEnt, ev);
            }
        }

        args = ev.Args;
    }
}
