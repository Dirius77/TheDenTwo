using Content.Shared.Body.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared.Body.Systems;

public sealed partial class BloodstreamSystem
{
    public FixedPoint2 GetBloodSolutionCapacity(Entity<BloodstreamComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp)
            || !_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
            return 0;

        return bloodSolution.MaxVolume;
    }
}
