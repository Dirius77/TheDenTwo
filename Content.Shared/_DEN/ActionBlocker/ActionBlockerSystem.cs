using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared.Speech;

namespace Content.Shared.ActionBlocker;

public sealed partial class ActionBlockerSystem
{
    [Dependency] private SharedLanguageSystem _language = null!;

    /// <summary>
    /// Whether a player is able to speak.
    /// This only checks if something blocks them from speaking, not if they had the ability to do so in the first place.
    /// </summary>
    /// <param name="uid">The mob to check.</param>
    public bool CanSpeak(EntityUid uid)
    {
        var languageEnt = _language.GetCurrentLanguageEntity(uid);

        // This one is used as broadcast
        var ev = new SpeakAttemptEvent(uid, languageEnt!.Value, null);
        RaiseLocalEvent(uid, ev, true);

        return !ev.Cancelled;
    }

}
