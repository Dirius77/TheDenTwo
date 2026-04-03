using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ModsuitControlComponent : Component
{
    private const string DefaultContainerId = "modsuit-modules";
    
    public Container ModuleContainer;
    
    /// <summary>
    /// The maximum Bus Width that this controller can support. Modules consume this based on their complexity.
    /// </summary>
    [DataField(required: true)] public int MaxBusWidth;
    
    /// <summary>
    /// The container ID to use for the Modsuit Modules.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ContainerId = DefaultContainerId;

    /// <summary>
    /// Maps bus 'slots' to modules in the ModuleContainer, used to make the UI show a consistent order.
    /// Multiple slots may be mapped to the same module based on its complexity.
    /// </summary>
    [AutoNetworkedField] public Dictionary<int, EntityUid?> ModuleSlots = new();

    /// <summary>
    /// Modules that will be pre-installed in this controller when it is spawned in. The complexity of these shouldn't
    /// exceed the maximum complexity of the controller.
    /// </summary>
    [DataField] public List<EntProtoId<ModsuitModuleComponent>> Preinstalled = new();
}

[Serializable, NetSerializable]
public sealed class ModuleSlotActionMessage(int slot) : BoundUserInterfaceMessage
{
    public int Slot = slot;
}

public sealed class ModuleRemovedFromControlEvent(Entity<ModsuitModuleComponent> module) : EntityEventArgs
{
    public Entity<ModsuitModuleComponent> Module = module;
}

public sealed class ModuleInsertedIntoControlEvent(Entity<ModsuitModuleComponent> module) : EntityEventArgs
{
    public Entity<ModsuitModuleComponent> Module = module;
}