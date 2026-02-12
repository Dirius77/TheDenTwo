using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;

namespace Content.Server._DEN.Language.EntitySystems;

public sealed class LanguageDebugSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<LanguageComponent, LanguageRelayedEvent<AttemptUnderstandingEvent>>(OnAttemptUnderstandingRelay);
    }

    private void OnAttemptUnderstandingRelay(Entity<LanguageComponent> ent, ref LanguageRelayedEvent<AttemptUnderstandingEvent> args)
    {
        Log.Debug("OUGH, GOT ATTEMPT UNDERSTANDING ON THE LANGUAGE ENTITY HELL YEAH.");
    }
}
