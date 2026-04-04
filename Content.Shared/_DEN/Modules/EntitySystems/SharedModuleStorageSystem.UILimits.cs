using Content.Shared._DEN.Modules.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Shared._DEN.Modules.EntitySystems;

public abstract partial class SharedModuleStorageSystem
{
    private void InitializeUILimits()
    {
        SubscribeLocalEvent<ModuleStorageBlockWornComponent, ModuleStorageUIOpenAttemptEvent>(OnBlockWornUIOpenAttempt);
        SubscribeLocalEvent<ModuleStorageBlockWornComponent, GotEquippedEvent>(OnBlockWornUIGotEquipped);
    }

    private void OnBlockWornUIGotEquipped(Entity<ModuleStorageBlockWornComponent> entity, ref GotEquippedEvent args)
    {
        _uiSystem.CloseUi(entity.Owner, ModuleUiKey.Key);
    }
    
    private void OnBlockWornUIOpenAttempt(Entity<ModuleStorageBlockWornComponent> entity,
        ref ModuleStorageUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        
        if (_inventorySystem.InSlotWithAnyFlags(entity.Owner, SlotFlags.WITHOUT_POCKET))
        {
            _popupSystem.PopupPredicted(Loc.GetString("module-storage-cannot-modify-while-worn", ("entity", entity)), entity, args.User);
            args.Cancel();
        }
    }
}