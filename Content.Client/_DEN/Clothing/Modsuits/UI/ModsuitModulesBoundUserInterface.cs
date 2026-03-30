using Content.Client._DEN.Clothing.Modsuits.UI.Controls;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using Robust.Client.UserInterface;

namespace Content.Client._DEN.Clothing.Modsuits.UI;

public sealed class ModsuitModulesBoundUserInterface : BoundUserInterface
{
    [ViewVariables] private ModsuitModuleWindow? _window;
    
    public ModsuitModulesBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        
        _window = this.CreateWindow<ModsuitModuleWindow>();
        if (EntMan.TryGetComponent(Owner, out ModsuitControlComponent? modControl))
            _window.BuildBackground(modControl.MaxBusWidth);
        Reload();
    }

    public void Reload()
    {
        if (_window == null || !EntMan.TryGetComponent(Owner, out ModsuitControlComponent? modControl))
            return;
        
        _window.Populate((Owner, modControl));
    }
}