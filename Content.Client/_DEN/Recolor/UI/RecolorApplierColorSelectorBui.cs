using Content.Shared._DEN.Recolor;
using Robust.Client.UserInterface;

namespace Content.Client._DEN.Recolor.UI;

public sealed class RecolorApplierColorSelectorBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private RecolorApplierColorSelectorMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<RecolorApplierColorSelectorMenu>();
        _menu.OnColorChanged += SelectColor;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        // Set the color of the color selector to the same color as the spray paint currently is
        if (state is RecolorSystem.RecolorApplierColorState recolorApplierColorState)
        {
            _menu?.SelectColor(recolorApplierColorState.Color);
        }
    }
    // Sent out when a new color is chosen
    private void SelectColor(Color color)
    {
        SendMessage(new RecolorSystem.RecolorApplierColorMessage(color));
    }
}
