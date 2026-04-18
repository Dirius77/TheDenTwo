using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Modules.Components;

/// <summary>
/// Similar to PowerCellDrawComponent except drawing from the modules holder instead of a cell in the entity itself.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModulePowerDrawComponent : Component
{
    /// <summary>
    /// Whether this module's power draw is enabled.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public bool Enabled;

    /// <summary>
    /// How much power the module draws while active. This value is subtracted, so negative values would be charging.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public float DrawRate = 1f;

    /// <summary>
    /// How much power is used when the module is 'activated', if it has a single activation instead of constant draw.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public float UseCharge;
}