using Content.Shared._DEN.Language;
using Content.Shared.Implants.Components;

namespace Content.Shared.Implants;

public abstract partial class SharedSubdermalImplantSystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<ImplantedComponent, LanguageAddedToCommunicatorEvent>(RelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, LanguageRemovedFromCommunicatorEvent>(RelayToImplantEvent);
    }
}
