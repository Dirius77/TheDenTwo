using Content.Shared._DEN.Language;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<InventoryComponent, LanguageAddedToCommunicatorEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, LanguageRemovedFromCommunicatorEvent>(RefRelayInventoryEvent);
    }
}
