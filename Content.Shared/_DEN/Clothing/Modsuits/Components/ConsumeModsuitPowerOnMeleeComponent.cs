using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

/// <summary>
/// Makes an entity drain power from something else (A modsuit) in order to deal damage with a melee hit.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConsumeModsuitPowerOnMeleeComponent : Component
{
    /// <summary>
    /// How much power is consumed when the attached entity hits something in melee. This is consumed once per hit event
    /// regardless of targets.
    /// </summary>
    [DataField, AutoNetworkedField] public float DrainOnMelee = 1.0f;

    /// <summary>
    /// Controls if power is consumed even when nothing is hit.
    /// </summary>
    [DataField] public bool MissesConsumePower = false;
    
    /// <summary>
    /// The inventory slot that the item will look in on the user for a modsuit to take power from.
    /// </summary>
    [DataField] public string ExpectedModsuitSlot = "back";
}