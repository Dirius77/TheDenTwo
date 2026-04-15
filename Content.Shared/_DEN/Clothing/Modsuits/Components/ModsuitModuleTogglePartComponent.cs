using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

/// <summary>
/// Causes a module to toggle the associated modsuit part when the module is toggled.
/// Does nothing without ModsuitPartAttachedModuleComponent
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ModsuitModuleTogglePartComponent : Component;