using Content.Server.Wires;
using Content.Shared._DEN.Wires.Components;
using Content.Shared.Emp;
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

        foreach (var wire in wires.WiresList)
        {
            if (_random.Prob(entity.Comp.Chance))
            {
            }
        }
    }
}