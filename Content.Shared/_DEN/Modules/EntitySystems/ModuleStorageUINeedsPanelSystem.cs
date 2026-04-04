using Content.Shared._DEN.Modules.Components;
using Content.Shared.Wires;

namespace Content.Shared._DEN.Modules.EntitySystems;

// This is its own system so that I can enforce ordering between this and the worn clothing check.
public sealed partial class ModuleStorageUINeedsPanelSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ModuleStorageNeedsPanelComponent, ModuleStorageUIOpenAttemptEvent>(
            OnNeedsPanelUIOpenAttempt, before: [typeof(SharedModuleStorageSystem)]);
    }
    
    private void OnNeedsPanelUIOpenAttempt(Entity<ModuleStorageNeedsPanelComponent> entity,
        ref ModuleStorageUIOpenAttemptEvent args)
    {
        if (TryComp<WiresPanelComponent>(entity, out var wiresPanel) && !wiresPanel.Open)
            args.Cancel();
    }
}