using Content.Client._DEN.Clothing.Modsuits.UI;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

namespace Content.Client._DEN.Clothing.Modsuits.EntitySystems;

public sealed partial class ModsuitSystem : SharedModsuitSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    
    protected override void UpdateUI(Entity<ModsuitControllerComponent> entity)
    {
        base.UpdateUI(entity);

        if (_uiSystem.TryGetOpenUi<ModsuitControllerBoundUserInterface>(entity.Owner, ModsuitControllerUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}