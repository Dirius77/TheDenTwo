using Content.Server.Wires;
using Content.Shared._DEN.Wires.Components;
using Content.Shared.Emp;
using Content.Shared.Wires;
using Robust.Shared.Random;

namespace Content.Server._DEN.Wires.EntitySystems;

public sealed class EmpWireFrySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly WiresSystem _wiresSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<EmpBreaksWiresComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnEmpPulse(Entity<EmpBreaksWiresComponent> entity, ref EmpPulseEvent evt)
    {
        if (!TryComp<WiresComponent>(entity, out var wires))
            return;

        var attemptEv = new AttemptEmpBreakWiresEvent();
        RaiseLocalEvent(entity, attemptEv);
        if (attemptEv.Cancelled)
            return;

        foreach (var wire in wires.WiresList)
        {
            if (_random.Prob(entity.Comp.Chance))
            {
                _wiresSystem.TryForceWireAction(entity, wire, WiresAction.Cut, wires);
            }
        }
    }
}