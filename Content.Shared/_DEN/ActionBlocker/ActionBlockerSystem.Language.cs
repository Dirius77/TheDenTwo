using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Speech;
using Content.Shared.Chat;

namespace Content.Shared.ActionBlocker;

public sealed partial class ActionBlockerSystem
{
    public bool CanSpeakLanguage(EntityUid uid, Entity<LanguageComponent> language, ChatChannel? channel = null)
    {
        var ev = new SpeakLanguageAttemptEvent(uid, language, channel);
        RaiseLocalEvent(uid, ev, true);

        return !ev.Cancelled;
    }
}
