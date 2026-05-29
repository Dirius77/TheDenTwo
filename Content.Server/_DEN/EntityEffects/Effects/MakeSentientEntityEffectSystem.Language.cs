using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class MakeSentientEntityEffectSystem
{
    [Dependency] private SharedLanguageSystem _languageSystem = default!;

    private static readonly ProtoId<LanguagePrototype> _animalLanugage = "Animal";

    private void MakeSentientLanguages(EntityUid target)
    {
        // Remove animal language if they had it before.
        _languageSystem.TryRemoveLanguage(target, _animalLanugage);
        
        // Add the default language, only if they don't already have it.
        var defaultLang = _languageSystem.GetDefaultLanguage();
        if (!_languageSystem.SpeaksLanguage(target, defaultLang))
            _languageSystem.TryAddLanguage(target, defaultLang, out _);
    }
}
