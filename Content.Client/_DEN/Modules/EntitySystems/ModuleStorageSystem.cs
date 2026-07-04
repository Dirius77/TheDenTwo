using Content.Shared._DEN.Modules.Components;
using Content.Shared._DEN.Modules.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client._DEN.Modules.EntitySystems;

public sealed partial class ModuleStorageSystem : SharedModuleStorageSystem
{
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModuleStorageComponent, AfterAutoHandleStateEvent>(OnModuleStorageAfterState);
    }

    protected override void UpdateUi(Entity<ModuleStorageComponent> entity)
    {
        if (!_uiSystem.TryGetOpenUi(entity.Owner, ModuleUiKey.Key,
                out var bui))
            return;
        
        bui.Update();
    }

    private void OnModuleStorageAfterState(Entity<ModuleStorageComponent> entity, ref AfterAutoHandleStateEvent evt)
    {
        UpdateUi(entity);
    }
}