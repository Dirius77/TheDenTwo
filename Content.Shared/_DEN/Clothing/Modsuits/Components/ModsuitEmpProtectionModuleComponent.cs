using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModsuitEmpProtectionModuleComponent : Component
{
    [DataField("broken", required: true)] public EntProtoId BrokenProtoId;
}