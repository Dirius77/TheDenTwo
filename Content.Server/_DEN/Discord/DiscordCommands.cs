using Content.Server.Administration.Managers;
using Content.Server.Discord.DiscordLink;
using Content.Shared._DEN.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server._DEN.Discord;

public sealed partial class DiscordCommands : IPostInjectInit
{
    [Dependency] private IAdminManager _adminManager = null!;
    [Dependency] private IConfigurationManager _config = null!;
    [Dependency] private IConsoleHost _consoleHost = null!;
    [Dependency] private DiscordLink _discordLink = null!;
    [Dependency] private IEntityManager _entityManager = null!;
    [Dependency] private ILogManager _log = null!;
    [Dependency] private IPlayerManager _playerManager = null!;

    private ISawmill _sawmill = null!;
    private List<ulong> _adminRoleIds = new();

    public void Initialize()
    {
        _config.OnValueChanged(DenCCVars.DiscordAdminRoleIds, OnDiscordAdminRoleIdsChanged, true);

        // Information
        _discordLink.RegisterCommandCallback(OnAdminwhoCommandRan, "adminwho");
        _discordLink.RegisterCommandCallback(OnCharactersCommandRun, "characters");
        _discordLink.RegisterCommandCallback(OnPlayersCommandRan, "players");

        // Moderation
        _discordLink.RegisterCommandCallback(OnKickCommandRan, "kick");
        _discordLink.RegisterCommandCallback(OnRespawnCommandRan, "respawn");
        _discordLink.RegisterCommandCallback(OnCallShuttleCommandRan, "callshuttle");
        _discordLink.RegisterCommandCallback(OnRecallShuttleCommandRan, "recallshuttle");

        // Sudo
        _discordLink.RegisterCommandCallback(OnExecuteCommandRan, "execute");
    }

    private void OnDiscordAdminRoleIdsChanged(string newAdminRoles)
    {
        var split = newAdminRoles.Split(",");

        _adminRoleIds.Clear();

        foreach (var newAdminRoleId in split)
        {
            var trimmedRoleId = newAdminRoleId.Trim();

            if (!ulong.TryParse(trimmedRoleId, out var adminRoleId))
                continue;

            _adminRoleIds.Add(adminRoleId);
        }
    }

    public void PostInject()
    {
        _sawmill = _log.GetSawmill("discord.commands");
    }
}
