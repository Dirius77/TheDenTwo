using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModsuitPieceComponent : Component
{
    [DataField, AutoNetworkedField] public EntityUid? ControllerId;
}