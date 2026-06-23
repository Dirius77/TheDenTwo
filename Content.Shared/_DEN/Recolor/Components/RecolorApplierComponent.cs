using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._DEN.Recolor.Components;

/// <summary>
/// Component used to designate something can apply recolors, EG spray paint.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RecolorApplierComponent : Component
{

    /// <summary>
    /// RecolorData to recolor items with.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RecolorData RecolorData;

    /// <summary>
    /// How long it takes for this object to apply the recolor to the target.
    /// </summary>
    [DataField]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(2.0f);

    /// <summary>
    /// Sound to play when the doafter is over.
    /// </summary>
    [DataField]
    public SoundSpecifier? DoAfterSound = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");

    /// <summary>
    /// Maximum amount of uses the applier can spray, if left null the applier can apply infinitely.
    /// </summary>
    [DataField]
    public int? MaxUses;

    /// <summary>
    /// Current amount of uses the applier can spray.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int UsesLeft;

    /// <summary>
    /// LocId used for the "you're outta paint!" popup.
    /// </summary>
    [DataField]
    public LocId NoMoreUsesPopup = "spray-paint-empty";

    /// <summary>
    /// LocId used for the "you can't paint that!" popup.
    /// </summary>
    [DataField]
    public LocId CantRecolorPopup = "spray-paint-fail";

    /// <summary>
    /// LocId used for the "you can't paint that!" popup.
    /// </summary>
    [DataField]
    public LocId ColorShowcaseExamine = "spray-paint-examine-color";

    /// <summary>
    /// LocId used for the "you can't paint that!" popup.
    /// </summary>
    [DataField]
    public LocId UsesExamine = "spray-paint-examine-uses";

    /// <summary>
    /// Entity Whitelist to determine what items can be repainted.
    /// </summary>
    [DataField]
    public EntityWhitelist? EntityWhitelist;

    /// <summary>
    /// Entity Blacklist to determine what items can't be repainted.
    /// </summary>
    [DataField]
    public EntityWhitelist? EntityBlacklist;

    /// <summary>
    /// LocId used for the apply recolor verb.
    /// </summary>
    [DataField]
    public LocId VerbText = "verb-spray-paint";

    /// <summary>
    /// Icon used for the apply recolor verb.
    /// </summary>
    [DataField]
    public SpriteSpecifier VerbIcon = new SpriteSpecifier.Texture(new ResPath("/Textures/_DEN/Interface/VerbIcons/paint-spray-can.svg.192dpi.png"));
}
