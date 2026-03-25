using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Clothing.Sealable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SealableClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsSealed = false;

    [DataField] 
    public string? SealedEquippedPrefix;
}
