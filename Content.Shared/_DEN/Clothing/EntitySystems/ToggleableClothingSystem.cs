using Content.Shared._DEN.Recolor;
using Content.Shared.Clothing.Components;

namespace Content.Shared.Clothing.EntitySystems;

public sealed partial class ToggleableClothingSystem
{
    [Dependency] private RecolorSystem _recolor = default!;

    private void OnToggleableRecolored(Entity<ToggleableClothingComponent> ent, ref OnRecoloredEvent args)
    {
        var toggled = ent.Comp.ClothingUid;

        if (toggled != null)
            _recolor.Recolor(toggled.Value, args.RecolorData, args.Recolorer);
    }

    private void OnToggleableRecolorRemoved(Entity<ToggleableClothingComponent> ent, ref OnRecolorRemovedEvent args)
    {
        var toggled = ent.Comp.ClothingUid;

        if (toggled != null)
            _recolor.RemoveRecolor(toggled.Value);
    }
}
