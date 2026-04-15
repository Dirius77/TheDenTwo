using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Modules.Components;

/// <summary>
/// Indicates that this entity is used to provide power to modules.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModulePowerProviderComponent : Component
{
    /// <summary>
    /// The base module drain is multiplied by this to determine the overall drain.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables] 
    public float DrainMultiplier = 1f;
}

/// <summary>
/// Used by the ModulePowerProvider to determine the total charge rate of the modules it contains.
/// </summary>
[ByRefEvent]
public record struct RefreshModuleChargeRateEvent
{
    public float ChargeRate;
}

/// <summary>
/// Used by the ModulePowerProvider to inform modules that there is no power left.
/// </summary>
[ByRefEvent]
public record struct ModulePowerDrainedEvent;

/// <summary>
/// Used to inform modules that power is restored.
/// </summary>
[ByRefEvent]
public record struct ModulePowerRestoredEvent;