using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModsuitControlComponent : Component
{
    public Container ModuleContainer;
    
    /// <summary>
    /// The maximum Bus Width that this controller can support. Modules consume this based on their complexity.
    /// </summary>
    [DataField(required: true), AutoNetworkedField] public int MaxBusWidth;

    /// <summary>
    /// Maps bus 'slots' to modules in the ModuleContainer, used to make the UI show a consistent order.
    /// Multiple slots may be mapped to the same module based on its complexity.
    /// </summary>
    [DataField, AutoNetworkedField] public Dictionary<int, EntityUid> ModuleSlots = new();

    /// <summary>
    /// Modules that will be pre-installed in this controller when it is spawned in. The complexity of these shouldn't
    /// exceed the maximum complexity of the controller.
    /// </summary>
    [DataField] public List<EntProtoId<ModsuitModuleComponent>> Preinstalled = new();
}