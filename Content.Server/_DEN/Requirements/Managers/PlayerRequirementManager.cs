using Content.Server.Preferences.Managers;
using Content.Shared._DEN.Requirements.Managers;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Robust.Shared.Player;

namespace Content.Server._DEN.Requirements.Managers;

/// <inheritdoc />
public sealed partial class PlayerRequirementManager : SharedPlayerRequirementManager
{
    [Dependency] private ISharedPlaytimeManager _playtimeManager = default!;
    [Dependency] private IServerPreferencesManager _prefsManager = default!;

    /// <inheritdoc />
    public override PlayerRequirementContext GetPlayerContext(ICommonSession session)
    {
        var playtimes = _playtimeManager.GetPlayTimes(session);
        var profile = GetProfile(session);

        return new()
        {
            Playtimes = playtimes,
            Profile = profile,
        };
    }

    /// <summary>
    ///     Retrieves a session's selected lobby character.
    /// </summary>
    /// <param name="session">The session to retrieve a profile for.</param>
    /// <returns>The session's currently-selected profile, if any.</returns>
    private HumanoidCharacterProfile? GetProfile(ICommonSession session)
    {
        if (!_prefsManager.TryGetCachedPreferences(session.UserId, out var prefs))
            return null;

        return prefs.SelectedCharacter;
    }
}
