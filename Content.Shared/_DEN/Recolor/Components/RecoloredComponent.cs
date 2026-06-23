using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Recolor.Components;

/// <summary>
/// Component used to designate that an item has been recolored, stores RecolorData for client to visualize.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RecoloredComponent : Component
{
    /// <summary>
    /// RecolorData this component is storing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RecolorData RecolorData;
}
