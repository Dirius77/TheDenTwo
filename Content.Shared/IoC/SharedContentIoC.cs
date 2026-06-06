using Content.Shared._DEN.Consent.Managers; // DEN - Consent system
using Content.Shared.Humanoid.Markings;
using Content.Shared.Localizations;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register(IDependencyCollection deps)
        {
            deps.Register<MarkingManager, MarkingManager>();
            deps.Register<ContentLocalizationManager, ContentLocalizationManager>();
            deps.Register<IConsentManager, ConsentManager>(); // DEN: Consent System
        }
    }
}
