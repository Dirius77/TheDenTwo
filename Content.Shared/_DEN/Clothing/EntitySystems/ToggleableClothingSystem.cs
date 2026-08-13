using Content.Shared._DEN.Recolor;
using Content.Shared.Clothing.Components;

namespace Content.Shared.Clothing.EntitySystems;

public sealed partial class ToggleableClothingSystem
{
    [Dependency] private RecolorSystem _recolor = default!;

    private void OnToggleableRecolored(Entity<ToggleableClothingComponent> ent, ref OnRecoloredEvent args)
    {
        var toggled = ent.Comp.ClothingUids.Keys;

        foreach (var piece in toggled)
        {
            _recolor.Recolor(piece, args.RecolorData, args.Recolorer);   
        }
    }

    private void OnToggleableRecolorRemoved(Entity<ToggleableClothingComponent> ent, ref OnRecolorRemovedEvent args)
    {
        var toggled = ent.Comp.ClothingUids.Keys;

        foreach (var piece in toggled)
        {
            _recolor.RemoveRecolor(piece);   
        }
    }
}
