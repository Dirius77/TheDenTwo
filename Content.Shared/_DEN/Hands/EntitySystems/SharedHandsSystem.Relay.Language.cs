using Content.Shared._DEN.Language;
using Content.Shared.Hands.Components;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<HandsComponent, LanguageAddedToCommunicatorEvent>(RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, LanguageRemovedFromCommunicatorEvent>(RefRelayEvent);
    }
}
