using Content.Server.Popups;
using Content.Server.Wires;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Modsuits.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Wires;

namespace Content.Server._DEN.Clothing.Modsuits.WireActions;

public sealed partial class SealableWireAction : ComponentWireAction<ModsuitControllerComponent>
{
    private SharedModsuitSystem _modsuitSystem = default!;
    private PopupSystem _popupSystem = default!;
    
    public override string Name { get; set; } = "wire-name-seals";
    public override Color Color { get; set; } = Color.Blue;
    public override object? StatusKey { get; } = SealableWireActionKey.StatusKey;

    public override void Initialize()
    {
        base.Initialize();
        
        _modsuitSystem = EntityManager.System<SharedModsuitSystem>();
        _popupSystem = EntityManager.System<PopupSystem>();
    }

    public override StatusLightState? GetLightState(Wire wire, ModsuitControllerComponent component)
        => component.PartsSpringlocked ? StatusLightState.BlinkingFast : StatusLightState.On;

    public override bool Cut(EntityUid user, Wire wire, ModsuitControllerComponent component)
    {
        _popupSystem.PopupEntity(Loc.GetString("springlock-wire-cut", ("name", wire.Owner)), wire.Owner, PopupType.LargeCaution);
        _modsuitSystem.TrySetSpringlocked((wire.Owner, component), true);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, ModsuitControllerComponent component)
    {
        _popupSystem.PopupEntity(Loc.GetString("springlock-wire-repaired", ("name", wire.Owner)), wire.Owner);
        _modsuitSystem.TrySetSpringlocked((wire.Owner, component), false);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, ModsuitControllerComponent component)
    {
        _popupSystem.PopupEntity(Loc.GetString("springlock-wire-pulsed", ("name", wire.Owner)), wire.Owner);
    }
}