using Content.Shared._DEN.Clothing.Modsuits.EntitySystems;
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

/// <summary>
/// Allows a modsuit module to provide armor to the modsuit. This component interacts with
/// ModsuitPartAttachedModuleComponent and ItemToggleComponent to handle being turned on and off and requiring specific
/// parts to be enabled. 
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ModsuitArmorSystem))]
public sealed partial class ModsuitArmorModuleComponent : Component
{
    /// <summary>
    /// Default damage modifier set. Applied to every part if there are multiple.
    /// </summary>
    [DataField, AutoNetworkedField] public DamageModifierSet? Modifiers;

    /// <summary>
    /// Per slot modifier sets. Use this if you need multiple parts and want the values to be different per part.s
    /// </summary>
    [DataField, AutoNetworkedField] public Dictionary<string, DamageModifierSet> SlotModifiers = new();
}