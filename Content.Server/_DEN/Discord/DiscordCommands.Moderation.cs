using System.Linq;
using System.Text;
using Content.Server.Discord.DiscordLink;
using NetCord;

namespace Content.Server._DEN.Discord;

public sealed partial class DiscordCommands
{
    private async void OnKickCommandRan(CommandReceivedEventArgs args)
    {
        if (!IsDiscordUserAdmin(args))
        {
            await args.Message.ReplyAsync("No permission.");
            return;
        }

        var cmdArgs = args.Arguments;

        if (cmdArgs.Count < 1)
        {
            await args.Message.ReplyAsync("Not enough arguments");
            return;
        }

        var target = cmdArgs[0];
        var reason = cmdArgs.Count > 1 ? cmdArgs[1] : "No reason provided.";

        if (!_playerManager.TryGetSessionByUsername(target, out var session))
        {
            await args.Message.ReplyAsync($"A player with the username of ``{target}` does not exist.");
            return;
        }

        _consoleHost.ExecuteCommand($"kick {target} {reason}");
        await args.Message.ReplyAsync("The player has been kicked.");
    }

    private async void OnRespawnCommandRan(CommandReceivedEventArgs args)
    {
        if (!IsDiscordUserAdmin(args))
        {
            await args.Message.ReplyAsync("No permission.");
            return;
        }

        var cmdArgs = args.Arguments;

        if (cmdArgs.Count < 1)
        {
            await args.Message.ReplyAsync("Not enough arguments");
            return;
        }

        var target = cmdArgs[0];

        if (!_playerManager.TryGetSessionByUsername(target, out var session))
        {
            await args.Message.ReplyAsync($"A player with the username of ``{target}` does not exist.");
            return;
        }

        _consoleHost.ExecuteCommand($"respawn {target}");
        await args.Message.ReplyAsync("The player has been respawned.");
    }

    private async void OnCallShuttleCommandRan(CommandReceivedEventArgs args)
    {
        if (!IsDiscordUserAdmin(args))
        {
            await args.Message.ReplyAsync("No permission.");
            return;
        }

        var cmdArgs = args.Arguments;
        var arrivalTime = string.Empty;

        if (cmdArgs.Count >= 1)
            arrivalTime = cmdArgs[0];

        _consoleHost.ExecuteCommand($"callshuttle {arrivalTime}");
        await args.Message.ReplyAsync("The shuttle has been called.");
    }

    private async void OnRecallShuttleCommandRan(CommandReceivedEventArgs args)
    {
        if (!IsDiscordUserAdmin(args))
        {
            await args.Message.ReplyAsync("No permission.");
            return;
        }

        _consoleHost.ExecuteCommand("recallshuttle");
        await args.Message.ReplyAsync("The shuttle has been recalled.");
    }

    private async void OnExecuteCommandRan(CommandReceivedEventArgs args)
    {
        if (args.Message.Author is not GuildUser guildUser
            || args.Message.Guild == null
            || args.Message.Channel == null
            || (guildUser.GetPermissions(args.Message.Guild) & Permissions.Administrator) == 0)
            return;

        var cmdArgs = args.Arguments;

        if (cmdArgs.Count < 1)
        {
            await args.Message.ReplyAsync("Not enough arguments.");
            return;
        }

        var executing = new StringBuilder();
        executing.AppendJoin(' ', cmdArgs);

        _consoleHost.ExecuteCommand($"{executing}");
        await args.Message.ReplyAsync($"Command ``{executing}`` was run successfully.");
    }
}
