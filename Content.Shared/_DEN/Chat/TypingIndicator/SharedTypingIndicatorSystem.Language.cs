using Content.Shared._DEN.Language.EntitySystems;

namespace Content.Shared.Chat.TypingIndicator;

public abstract partial class SharedTypingIndicatorSystem
{
    [Dependency] private SharedLanguageSystem _language = default!;

    private void OnTypingLanguageChanged(TypingChangedEvent ev, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;
        if (!Exists(uid))
        {
            Log.Warning($"Client {args.SenderSession} sent TypingChangedEvent without an attached entity.");
            return;
        }

        var languageEnt = _language.GetCurrentLanguageEntity(uid.Value);
        // Don't send typing if they have no valid language.
        // See DenCCVars.FallbackDefaultLanguage if you need some weird entity to have an indicator.
        if (languageEnt == null)
            return;

        // check if this entity can speak or emote
        if (!_actionBlocker.CanEmote(uid.Value) && !_actionBlocker.CanSpeakLanguage(uid.Value, languageEnt.Value))
        {
            // nah, make sure that typing indicator is disabled
            SetTypingIndicatorState(uid.Value, TypingIndicatorState.None);
            return;
        }

        SetTypingIndicatorState(uid.Value, ev.State);
    }
}
