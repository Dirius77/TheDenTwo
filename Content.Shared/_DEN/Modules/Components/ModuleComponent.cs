using Content.Shared.Whitelist;

namespace Content.Shared._DEN.Modules.Components;

[RegisterComponent]
public sealed partial class ModuleComponent : Component
{
    /// <summary>
    /// How much 'bus space' this module consumes when installed.
    /// </summary>
    [DataField(required: true)] public int BusWidth;

    [DataField("texture", required: true)] public string UITexture;

    /// <summary>
    /// Whitelist that at least one already installed module must pass to allow this module to be installed.
    /// These should be symmetrical between modules because only the one being installed is ever checked.
    /// </summary>
    [DataField] public EntityWhitelist? SiblingWhitelist;
    
    /// <summary>
    /// Blacklist that all installed modules must pass to allow this module to be installed.
    /// These should be symmetrical between modules because only the one being installed is ever checked.
    /// </summary>
    [DataField] public EntityWhitelist? SiblingBlacklist;

    /// <summary>
    /// The module storage that this module is currently inside.
    /// </summary>
    [DataField] public EntityUid? StoredIn;
}

/// <summary>
/// Raised on the module when it is inserted into a ModuleStorage
/// </summary>
/// <param name="storage">The storage it was inserted into.</param>
public sealed class ModuleRemovedEvent(Entity<ModuleStorageComponent> storage) : EntityEventArgs
{
    public Entity<ModuleStorageComponent> Storage = storage;
}

/// <summary>
/// Raised on the module when it is removed from a ModuleStorage
/// </summary>
/// <param name="storage">The storage it was removed from.</param>
public sealed class ModuleInsertedEvent(Entity<ModuleStorageComponent> storage) : EntityEventArgs
{
    public Entity<ModuleStorageComponent> Storage = storage;
}