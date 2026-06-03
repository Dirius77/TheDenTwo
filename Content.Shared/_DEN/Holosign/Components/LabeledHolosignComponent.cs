using Content.Shared._DEN.Holosign.Systems;
using Robust.Shared.GameStates;


namespace Content.Shared._DEN.Holosign.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedLabelableHolosignProjectorSystem))]
public sealed partial class LabeledHolosignComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Description;

    [DataField, AutoNetworkedField]
    public bool IsNSFW;
}
