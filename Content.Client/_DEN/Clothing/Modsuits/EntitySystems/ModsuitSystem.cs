using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Modsuits.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client._DEN.Clothing.Modsuits.EntitySystems;

public sealed partial class ModsuitSystem : SharedModsuitSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModsuitControlComponent, AfterAutoHandleStateEvent>(OnModsuitControlAfterState);
    }

    protected override void UpdateUi(Entity<ModsuitControlComponent> entity)
    {
        if (!_uiSystem.TryGetOpenUi(entity.Owner, ModsuitModuleUiKey.Key,
                out var bui))
            return;
        
        bui.Update();
    }

    private void OnModsuitControlAfterState(Entity<ModsuitControlComponent> entity, ref AfterAutoHandleStateEvent evt)
    {
        UpdateUi(entity);
    }
}