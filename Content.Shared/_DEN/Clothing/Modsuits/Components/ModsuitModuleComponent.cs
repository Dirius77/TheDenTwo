namespace Content.Shared._DEN.Clothing.Modsuits.Components;

/// <summary>
/// Indicates that a module is one that belongs to a modsuit.
/// </summary>
[RegisterComponent]
public sealed partial class ModsuitModuleComponent : Component
{
    public Entity<ModsuitControllerComponent>? ModController;
}