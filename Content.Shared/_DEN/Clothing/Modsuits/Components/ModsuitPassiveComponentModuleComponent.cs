using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

/// <summary>
/// Causes a set of components to be added to, removed from, or prevented from being added to the parts in
/// the provided slots when they are sealed or unsealed. There is no enforcement of a particular order for these modules.
/// If two modules are going to modify the same set of components then they should likely be prevented from being
/// installed together. See ModuleComponent's blacklist and whitelist.
///
/// Relevant slots / parts are determined from ModsuitPartAttachedModuleComponent. If you do not have one this 
/// </summary>
[RegisterComponent]
public sealed partial class ModsuitPassiveComponentModuleComponent : Component
{
    /// <summary>
    /// These components are prevented from being added to the part on seal if they otherwise would have been.
    /// </summary>
    [DataField] public ComponentRegistry? BlockOnSeal;
    
    /// <summary>
    /// These components are prevented from being added to the part on unseal if they otherwise would have been.
    /// </summary>
    [DataField] public ComponentRegistry? BlockOnUnseal;
    
    /// <summary>
    /// These components are added to the part on seal. They are removed on unseal
    /// </summary>
    [DataField] public ComponentRegistry? AddOnSeal;
    
    /// <summary>
    /// These components are added to the part on unseal. They are removed on seal.
    /// </summary>
    [DataField] public ComponentRegistry? AddOnUnseal;
}