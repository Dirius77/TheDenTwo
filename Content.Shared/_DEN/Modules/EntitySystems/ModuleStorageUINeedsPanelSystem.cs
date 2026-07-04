using Content.Shared._DEN.Modules.Components;
using Content.Shared.Wires;

namespace Content.Shared._DEN.Modules.EntitySystems;

// This is its own system so that I can enforce ordering between this and the worn clothing check.
public sealed partial class ModuleStorageUINeedsPanelSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<ModuleStorageNeedsPanelComponent, ModuleStorageUIOpenAttemptEvent>(
            OnNeedsPanelUIOpenAttempt, before: [typeof(SharedModuleStorageSystem)]);
        SubscribeLocalEvent<ModuleStorageNeedsPanelComponent, PanelChangedEvent>(OnPanelChanged);
    }
    
    private void OnNeedsPanelUIOpenAttempt(Entity<ModuleStorageNeedsPanelComponent> entity,
        ref ModuleStorageUIOpenAttemptEvent args)
    {
        if (TryComp<WiresPanelComponent>(entity, out var wiresPanel) && !wiresPanel.Open)
            args.Cancel();
    }

    private void OnPanelChanged(Entity<ModuleStorageNeedsPanelComponent> entity, ref PanelChangedEvent args)
    {
        if (!args.Open)
        {
            _uiSystem.CloseUi(entity.Owner, ModuleUiKey.Key);
        }
    }
}