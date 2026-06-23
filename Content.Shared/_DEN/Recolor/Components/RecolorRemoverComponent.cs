using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Recolor.Components;

/// <summary>
/// Component used to designate that an item can remove RecolorComponent from applicable recolored items.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RecolorRemoverComponent : Component
{
    /// <summary>
    /// How long it takes for this object to remove the recolor on the target.
    /// </summary>
    [DataField]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(2.0f);

    /// <summary>
    /// Sound to play when the doafter is over.
    /// </summary>
    [DataField]
    public SoundSpecifier? DoafterSound;

    /// <summary>
    /// Only these entities will be able to have their recolor removed by this item.
    /// </summary>
    [DataField]
    public EntityWhitelist? EntityWhitelist;

    /// <summary>
    /// These entities will not be able to have their recolor removed by this item.
    /// </summary>
    [DataField]
    public EntityWhitelist? EntityBlacklist;
}
