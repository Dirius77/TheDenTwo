using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Sealable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SealableClothingComponent : Component
{
    [DataField, AutoNetworkedField] public bool IsSealed = false;

    [DataField] public List<string> SealedSpriteLayers = [];
    
    [DataField] public TimeSpan SealDoAfterTime = TimeSpan.FromSeconds(1.5);

    [DataField("requiredSlot", true)] public SlotFlags RequiredFlags;
    
    [DataField] public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();

    [DataField] public LocId SealMessage = "sealable-clothing-started-sealing";

    [DataField] public LocId UnsealMessage = "sealable-clothing-started-unsealing";
    
    /// <summary>
    /// Default set of components that are added when this piece is sealed.
    /// </summary>
    [DataField] public ComponentRegistry? SealedAddComponents;
    
    /// <summary>
    /// Default set of components that are removed when this piece is sealed.
    /// </summary>
    [DataField] public ComponentRegistry? SealedRemoveComponents;
    
    /// <summary>
    /// Default set of components that are added when this piece is unsealed.
    /// </summary>
    [DataField] public ComponentRegistry? UnsealedAddComponents;
    
    /// <summary>
    /// Default set of components that are removed when this piece is unsealed.
    /// </summary>
    [DataField] public ComponentRegistry? UnsealedRemoveComponents;
}
