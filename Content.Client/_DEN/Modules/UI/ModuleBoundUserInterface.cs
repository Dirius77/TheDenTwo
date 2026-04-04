using Content.Client._DEN.Modules.UI.Controls;
using Content.Shared._DEN.Modules.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._DEN.Modules.UI;

// I would rather this be called ModuleStorageBoundUserInterface however it cannot be called that because 
// it's too close to StorageBoundUserInterface and that makes the reflection system pick the wrong BoundUserInterface
// to construct when building UI's for backpacks.
[UsedImplicitly]
public sealed class ModuleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables] private ModuleStorageWindow? _window;

    protected override void Open()
    {
        base.Open();
        
        _window = this.CreateWindow<ModuleStorageWindow>();
        if (EntMan.TryGetComponent(Owner, out ModuleStorageComponent? modControl))
            _window.Initialize((Owner, modControl));
        _window.OnSlotClicked += ModuleInsertAttempt;
        Update();
    }

    public override void Update()
    {
        if (_window == null || !EntMan.TryGetComponent(Owner, out ModuleStorageComponent? modControl))
            return;
        
        _window.Update();
    }

    private void ModuleInsertAttempt(int slot)
    {
        SendPredictedMessage(new ModuleSlotActionMessage(slot));
    }
}