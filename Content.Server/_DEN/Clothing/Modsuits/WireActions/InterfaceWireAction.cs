using Content.Server.Wires;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Modsuits.EntitySystems;
using Content.Shared.Wires;
using JetBrains.Annotations;

namespace Content.Server._DEN.Clothing.Modsuits.WireActions;

[UsedImplicitly]
public sealed partial class InterfaceWireAction : ComponentWireAction<ModsuitControllerComponent>
{
    private SharedModsuitSystem _modsuitSystem = default!;
    
    public override string Name { get; set; } = "wire-name-interface";
    public override Color Color { get; set; } = Color.Blue;
    public override object? StatusKey { get; } = InterfaceWireActionKey.StatusKey;

    public override void Initialize()
    {
        base.Initialize();
        
        _modsuitSystem = EntityManager.System<SharedModsuitSystem>();
    }

    public override StatusLightState? GetLightState(Wire wire, ModsuitControllerComponent component)
        => component.UIFunctional ? StatusLightState.On : StatusLightState.Off;

    public override bool Cut(EntityUid? user, Wire wire, ModsuitControllerComponent component)
    {
        _modsuitSystem.SetUIFunctionality((wire.Owner, component), false);
        return true;
    }

    public override bool Mend(EntityUid? user, Wire wire, ModsuitControllerComponent component)
    {
        _modsuitSystem.SetUIFunctionality((wire.Owner, component), true);
        return true;
    }

    public override void Pulse(EntityUid? user, Wire wire, ModsuitControllerComponent component)
    {
    }
}