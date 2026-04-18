using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Modules.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ModuleStorageComponent : Component
{
    private const string DefaultContainerId = "modules";
    
    public Container ModuleContainer;
    
    /// <summary>
    /// The maximum Bus Width that this storage can support. Modules consume this based on their complexity.
    /// </summary>
    [DataField(required: true)] public int MaxBusWidth;
    
    /// <summary>
    /// The container ID to use for the modules.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ContainerId = DefaultContainerId;

    /// <summary>
    /// What kind of tool quality is needed to remove things from this storage. This can be set to null to not need one.
    /// </summary>
    [DataField, AutoNetworkedField] public ProtoId<ToolQualityPrototype>? RemovalQuality = "Prying";

    /// <summary>
    /// Maps bus 'slots' to modules in the ModuleContainer, used to make the UI show a consistent order.
    /// Multiple slots may be mapped to the same module based on its complexity.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)] 
    public Dictionary<int, EntityUid?> ModuleSlots = new();

    /// <summary>
    /// The sound that is played when a module is inserted into this storage.
    /// </summary>
    [DataField] public SoundSpecifier? InsertSound =
        new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

    /// <summary>
    /// Modules must pass this whitelist in order to be installed.
    /// </summary>
    [DataField] public EntityWhitelist? Whitelist;
    
    /// <summary>
    /// Modules must pass this blacklist in order to be installed.
    /// </summary>
    [DataField] public EntityWhitelist? Blacklist;
}

/// <summary>
/// Sent from the client to the server (as well as prediction on the client) to demonstrate intent to interact with
/// a slot in the module storage. The system will look at the user's active items and the state of the slot and use
/// that to determine the correct action to take (inserting a module, removing a module, or doing nothing).
/// </summary>
/// <param name="slot">The slot the user is trying to interact with.</param>
[Serializable, NetSerializable]
public sealed class ModuleSlotActionMessage(int slot) : BoundUserInterfaceMessage
{
    public int Slot = slot;
}

/// <summary>
/// Raised on the ModuleStorage when a module is removed from it.
/// </summary>
/// <param name="module">The module that was removed.</param>
public sealed class ModuleRemovedFromStorageEvent(Entity<ModuleComponent> module) : EntityEventArgs
{
    public Entity<ModuleComponent> Module = module;
}

/// <summary>
/// Raised on the ModuleStorage entity when a module is inserted into it.
/// </summary>
/// <param name="module">The module entity that was inserted.</param>
public sealed class ModuleInsertedIntoStorageEvent(Entity<ModuleComponent> module) : EntityEventArgs
{
    public Entity<ModuleComponent> Module = module;
}

/// <summary>
/// Raised on the module storage entity when a user tries to open its UI.
/// </summary>
/// <param name="user">The user attempting to open the UI.</param>
public sealed class ModuleStorageUIOpenAttemptEvent(EntityUid user)
    : CancellableEntityEventArgs
{
    public EntityUid User = user;
}

/// <summary>
/// Raised on a module to verify that it can be inserted into a storage. The storage has already checked to ensure that
/// the module fits at this point.
/// </summary>
/// <param name="storage">The storage the module is being put into.</param>
public sealed class ModuleGettingInsertedAttemptEvent(Entity<ModuleStorageComponent> storage) : CancellableEntityEventArgs
{
    public Entity<ModuleStorageComponent> Storage = storage;
}

/// <summary>
/// Raised on a storage to verify that a module can be inserted into it. The storage has already checked to ensure that
/// the module fits at this point.
/// </summary>
/// <param name="module">The module trying to be inserted.</param>
public sealed class AttemptInsertModuleIntoStorageEvent(Entity<ModuleComponent> module) : CancellableEntityEventArgs
{
    public Entity<ModuleComponent> Module = module;
}