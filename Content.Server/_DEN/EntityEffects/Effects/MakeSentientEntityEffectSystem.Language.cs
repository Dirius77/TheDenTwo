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
        _languageSystem.TryRemoveLanguage(target, _animalLanugage);
        var defaultLang = _languageSystem.GetDefaultLanguage();
        if (!_languageSystem.SpeaksLanguage(target, defaultLang))
            _languageSystem.TryAddLanguage(target, defaultLang, out _);
    }
}
