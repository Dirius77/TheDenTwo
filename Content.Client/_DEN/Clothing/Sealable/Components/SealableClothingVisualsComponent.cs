namespace Content.Client._DEN.Clothing.Sealable.Components;

[RegisterComponent]
public sealed partial class SealableClothingVisualsComponent : Component
{
    [DataField] public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();
}