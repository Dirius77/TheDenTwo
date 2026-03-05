using Content.Shared.Whitelist;

namespace Content.Shared._DEN.Language.Components;

/// <summary>
///     Gestalt languages transmit to all available players and rely on the languages understanding to filter
///     out players who do not speak the language.
/// </summary>
[RegisterComponent]
public sealed partial class GestaltComponent : Component
{
    [DataField]
    public bool RequiresHost;

    [DataField]
    public EntityWhitelist? ReceiverWhitelist;

    /// <summary>
    ///     Entity whitelist that a host must pass to count. Hosts must also always have a GestaltHostComponent just so
    ///     that GestaltSystem doesn't have to search every entity in the game for a host. If a host can be alive, it
    ///     must be alive.
    /// </summary>
    [DataField]
    public EntityWhitelist? HostWhitelist;

    [DataField]
    public List<LocId> MissingHostPopups = [];
}
