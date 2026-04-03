using Content.Client._DEN.Clothing.Modsuits.UI.Controls;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._DEN.Clothing.Modsuits.UI;

[UsedImplicitly]
public sealed class ModsuitModulesBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables] private ModsuitModuleWindow? _window;

    protected override void Open()
    {
        base.Open();
        
        _window = this.CreateWindow<ModsuitModuleWindow>();
        if (EntMan.TryGetComponent(Owner, out ModsuitControlComponent? modControl))
            _window.Initialize((Owner, modControl));
        _window.OnSlotClicked += ModuleInsertAttempt;
        Update();
    }

    public override void Update()
    {
        if (_window == null || !EntMan.TryGetComponent(Owner, out ModsuitControlComponent? modControl))
            return;
        
        _window.Update();
    }

    private void ModuleInsertAttempt(int slot)
    {
        SendPredictedMessage(new ModuleSlotActionMessage(slot));
    }
}