using Content.Client._DEN.Clothing.Modsuits.UI.Controls;
using Content.Shared._DEN.Clothing.Modsuits.EntitySystems;
using Content.Shared._DEN.Modules.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._DEN.Clothing.Modsuits.UI;

[UsedImplicitly]
public sealed class ModsuitControllerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables] private ModsuitControllerWindow? _window;
    
    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ModsuitControllerWindow>();
        if (EntMan.TryGetComponent(Owner, out ModuleStorageComponent? modStorage))
            _window.Initialize((Owner, modStorage));
        _window.OnModuleToggled += OnModuleToggled;
        Update();
    }

    public override void Update()
    {
        if (_window == null || !EntMan.TryGetComponent(Owner, out ModuleStorageComponent? modControl))
            return;
        
        _window.Update();
    }

    private void OnModuleToggled(EntityUid entity, bool enabled)
    {
        var netEnt = EntMan.GetNetEntity(entity);
        SendPredictedMessage(new ModsuitToggleModuleMessage(netEnt, enabled));
    }
}