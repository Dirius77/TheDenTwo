using Content.Client.Lobby;
using Content.Shared._DEN.Requirements.Managers;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._DEN.Requirements.Managers;

/// <inheritdoc />
public sealed partial class PlayerRequirementManager : SharedPlayerRequirementManager
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IClientPreferencesManager _preferences = default!;
    [Dependency] private ISharedPlaytimeManager _playtimeManager = default!;

    /// <inheritdoc />
    public override PlayerRequirementContext GetPlayerContext(ICommonSession session)
    {
        if (_player.LocalSession != session)
            return new();

        var playtimes = _playtimeManager.GetPlayTimes(session);
        var profile = _preferences.Preferences?.SelectedCharacter;

        return new()
        {
            Playtimes = playtimes,
            Profile = profile,
        };
    }
}
