using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

/// <summary>
/// Causes a set of components to be toggled on the wearer entity of the modsuit when this module is toggled.
/// </summary>
[RegisterComponent]
public sealed partial class ModsuitModuleToggleParentComponentsComponent : Component
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