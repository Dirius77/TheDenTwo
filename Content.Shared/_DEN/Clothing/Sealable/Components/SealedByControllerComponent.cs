using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Clothing.Sealable.Components;

/// <summary>
/// Marks a component as interacting with a controller so that it can get relevant events.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SealedByControllerComponent : Component
{
    [DataField, AutoNetworkedField] public EntityUid? Controller;
}