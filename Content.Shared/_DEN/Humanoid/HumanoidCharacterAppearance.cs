using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidCharacterAppearance
{
    private static void EnsureSkinColorValid(HumanoidCharacterAppearance appearance,
        SpeciesPrototype species,
        IPrototypeManager proto)
    {
        var skinColor = proto.Index(species.SkinColoration);


    }
}
