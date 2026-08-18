using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void SetSkinColoration(string speciesId)
    {
        if (Profile == null)
            return;

        if (!_prototypeManager.Resolve<SpeciesPrototype>(speciesId, out var _))
            return;

        var protoId = (ProtoId<SpeciesPrototype>)speciesId;
        SkinColorSelector.SetSkinColoration(protoId);
    }

    private void DenUpdateSkinColor()
    {
        if (Profile is null)
            return;

        var skinColor = Profile.Appearance.SkinColor;
        SkinColorSelector.SetSkinColor(skinColor);
    }

    private void DenOnSkinColorOnValueChanged(Color color)
    {
        if (Profile is null)
            return;

        _markingsModel.SetOrganSkinColor(color);
        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));
        ReloadProfilePreview();
    }
}
