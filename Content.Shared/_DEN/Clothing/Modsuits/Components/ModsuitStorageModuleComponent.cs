using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

[RegisterComponent]
public sealed partial class ModsuitStorageModuleComponent : Component
{
    [DataField] public ComponentRegistry StorageComponent;
}