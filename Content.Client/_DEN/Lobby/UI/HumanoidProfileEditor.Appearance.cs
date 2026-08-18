using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void DenSetSkinColoration(string speciesId)
    {
        if (Profile == null)
            return;

        if (!_prototypeManager.Resolve<SpeciesPrototype>(speciesId, out var _))
            return;

        var protoId = (ProtoId<SpeciesPrototype>)speciesId;
        SkinColorSelector.SetSkinColoration(protoId);

        // The skin color might change upon setting the species,
        // so we are making sure that the appearance and markings reflect this.
        DenOnSkinColorValueChanged();
    }

    private void DenUpdateSkinColor()
    {
        if (Profile is null)
            return;

        var skinColor = Profile.Appearance.SkinColor;
        SkinColorSelector.SetSkinColor(skinColor);
    }

    private void DenOnSkinColorValueChanged()
    {
        if (Profile is null)
            return;

        var color = SkinColorSelector.Color;
        _markingsModel.SetOrganSkinColor(color);
        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));
        ReloadProfilePreview();
    }
}
