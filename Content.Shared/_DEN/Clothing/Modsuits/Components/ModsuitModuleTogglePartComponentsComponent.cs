using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

/// <summary>
/// Causes a set of components to be added to the part this module is associated with when the module is toggled.
///
/// Uses ModsuitPartAttachedModuleComponent to be associated with a part.
/// </summary>
// TODO: This should be more generic.
[RegisterComponent]
public sealed partial class ModsuitModuleTogglePartComponentsComponent : Component
{
    /// <summary>
    /// Components that are added to the part on toggle. These will be removed when toggled off.
    /// </summary>
    [DataField] public ComponentRegistry? AddOnToggle;
    
    /// <summary>
    /// Components that are removed from the part on toggle. These will be added when toggled off.
    /// </summary>
    [DataField] public ComponentRegistry? RemoveOnToggle;
}