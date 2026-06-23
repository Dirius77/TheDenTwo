using Content.Shared._DEN.Recolor.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Recolor;

public sealed partial class RecolorSystem
{
    private void OnBoundUIOpened(Entity<RecolorApplierColorSelectorComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not RecolorApplierColorSelectorKey key
            || !TryComp<RecolorApplierComponent>(ent.Owner, out var recolorApplier))
            return;

        var state = new RecolorApplierColorState(recolorApplier.RecolorData.Color);
        _ui.SetUiState(ent.Owner, key, state);
    }

    private void OnRecolorApplierColorChanged(Entity<RecolorApplierColorSelectorComponent> ent, ref RecolorApplierColorMessage args)
    {
        if (!TryComp<RecolorApplierComponent>(ent, out var recolorApplier))
            return;

        ChangeColor((ent.Owner, recolorApplier), args.Color);
    }

    [Serializable, NetSerializable]
    public enum RecolorApplierColorSelectorKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class RecolorApplierColorMessage(Color color) : BoundUserInterfaceMessage
    {
        public readonly Color Color = color;
    }

    [Serializable, NetSerializable]
    public sealed class RecolorApplierColorState(Color color) : BoundUserInterfaceState
    {
        public readonly Color Color = color;
    }
}
