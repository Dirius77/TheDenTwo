using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._DEN.Language.EntitySystems;

// If this gets moved to shared it breaks the client UI because it creates a clientside only language.
public sealed class UniversalLanguageSpeakerSystem : EntitySystem
{
    [Dependency] private readonly SharedLanguageSystem _language = default!;

    private static readonly ProtoId<LanguagePrototype> Universal = "Universal";

    public override void Initialize()
    {
        SubscribeLocalEvent<UniversalLanguageSpeakerComponent, ComponentStartup>(OnUniversalLanguageStartup);
        SubscribeLocalEvent<UniversalLanguageSpeakerComponent, AttemptUnderstandingEvent>(
            OnUniversalAttemptUnderstanding);
    }

    private void OnUniversalLanguageStartup(Entity<UniversalLanguageSpeakerComponent> entity, ref ComponentStartup args)
    {
        if (_language.TryAddLanguage(entity, Universal, SharedLanguageSystem.MaximumFluency, true, out var langs))
        {
            if (langs.FirstOrNull() is { } lang && TryComp<LanguageComponent>(lang, out var langComp))
            {
                entity.Comp.UniversalLanguage = (lang, langComp);
                return;
            }
        }
        Log.Debug("Failed to add universal language to: " + Name(entity));
    }

    private void OnUniversalAttemptUnderstanding(Entity<UniversalLanguageSpeakerComponent> ent,
        ref AttemptUnderstandingEvent evt)
    {
        evt.Understanding = ent.Comp.UniversalLanguage;
        evt.Handled = true;
    }
}
