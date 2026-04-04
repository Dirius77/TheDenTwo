using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Modules.Components;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Server._DEN.Clothing.Modsuits.EntitySystems;

public sealed partial class ModsuitStorageModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _interfaceSystem = default!;
    
    private const string StorageBoundUserInterface = "StorageBoundUserInterface";
    
    public override void Initialize()
    {
        SubscribeLocalEvent<ModsuitStorageModuleComponent, ModuleInsertedEvent>(OnModuleInserted);
        SubscribeLocalEvent<ModsuitStorageModuleComponent, ModuleRemovedEvent>(OnModuleRemoved);
    }

    private void OnModuleInserted(Entity<ModsuitStorageModuleComponent> entity, ref ModuleInsertedEvent args)
    {
        var target = args.Storage;

        // Don't try to do this if it's already a storage.
        if (HasComp<StorageComponent>(target))
            return;
        
        EntityManager.AddComponents(target, entity.Comp.StorageComponent);

        if (!_interfaceSystem.HasUi(target, StorageComponent.StorageUiKey.Key))
        {
            _interfaceSystem.SetUi(target.Owner, StorageComponent.StorageUiKey.Key, new InterfaceData(StorageBoundUserInterface));
        }

        DirtyEntity(target);
    }

    private void OnModuleRemoved(Entity<ModsuitStorageModuleComponent> entity, ref ModuleRemovedEvent args)
    {
        var coordinates = Transform(args.Storage).Coordinates;
        var target = args.Storage;

        if (TryComp<StorageComponent>(target, out var storage))
        {
            var container = storage.Container;
            RemComp<StorageComponent>(target);
            _container.EmptyContainer(container, destination: coordinates);
            _container.ShutdownContainer(container);
        }
    }
}