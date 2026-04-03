using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

[RegisterComponent]
public sealed partial class ModsuitModuleComponent : Component
{
    /// <summary>
    /// How much 'bus space' this module consumes when installed.
    /// </summary>
    [DataField(required: true)] public int BusWidth;

    [DataField("texture", required: true)] public string UITexture;
}

public sealed class ModuleRemovedEvent(Entity<ModsuitControlComponent> control) : EntityEventArgs
{
    public Entity<ModsuitControlComponent> Control = control;
}

public sealed class ModuleInsertedEvent(Entity<ModsuitControlComponent> control) : EntityEventArgs
{
    public Entity<ModsuitControlComponent> Control = control;
}