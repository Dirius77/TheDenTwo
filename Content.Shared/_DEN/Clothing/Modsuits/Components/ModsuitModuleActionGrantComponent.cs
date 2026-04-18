using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModsuitModuleActionGrantComponent : Component
{
    [DataField(required: true)] public EntProtoId<InstantActionComponent> Action;

    [DataField, AutoNetworkedField] public EntityUid? ActionEntity;
}