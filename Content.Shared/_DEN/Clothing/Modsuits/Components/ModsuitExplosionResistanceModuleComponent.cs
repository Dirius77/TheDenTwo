using Content.Shared._DEN.Clothing.Modsuits.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

/// <summary>
/// Used for a modsuit module which modifies the explosion resistance of attached pieces of the modsuit.s
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ModsuitArmorSystem))]
public sealed partial class ModsuitExplosionResistanceModuleComponent : Component
{
    /// <summary>
    /// The damage coefficient applied to all attached parts if present.
    /// </summary>
    [DataField, AutoNetworkedField] public float? DamageCoefficient = 1.0f;

    /// <summary>
    /// Per slot explosion modifiers, these are preferred over DamageCoefficient if present.
    /// </summary>
    [DataField, AutoNetworkedField] public Dictionary<string, float> SlotCoefficients = new();
}