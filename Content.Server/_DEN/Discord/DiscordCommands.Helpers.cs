using System.Linq;
using System.Text;
using Content.Server.Administration.Systems;
using Content.Server.Discord.DiscordLink;
using Content.Server.Mind;
using NetCord;
using Robust.Shared.Player;

namespace Content.Server._DEN.Discord;

public sealed partial class DiscordCommands
{
    private Dictionary<string, string> _emojis = new()
    {
        {"antagonist", ":knife:"},
        {"lobby", ":clock1:"},
        {"adminned", ":heart:"},
        {"deadminned", ":broken_heart:"}
    };

    private string GetAdminListText(ICommonSession session)
    {
        var adminned = _adminManager.IsAdmin(session) ? _emojis["adminned"] : _emojis["deadminned"];
        var username = session.Data.UserName;

        return $"{adminned} | {username}";
    }

    private string GetPlayerListText(ICommonSession session)
    {
        var sentText = new StringBuilder();

        if (session.AttachedEntity is not { Valid: true } attachedEntity
            || !_entityManager.TryGetComponent<MetaDataComponent>(attachedEntity, out var metaData))
        {
            sentText.AppendJoin(' ', _emojis["lobby"]);
            sentText.AppendJoin(' ', $"- {session.Data.UserName}");

            return sentText.ToString();
        }
        var mindSystem = _entityManager.System<MindSystem>();
        mindSystem.TryGetMind(session, out _, out var mind);

        var adminSystem = _entityManager.System<AdminSystem>();
        var cachedPlayerInfo = mind != null && mind.UserId != null ? adminSystem.GetCachedPlayerInfo(mind.UserId.Value) : null;
        var antag = mind?.UserId != null && (cachedPlayerInfo?.Antag ?? false);

        var isAdmin = _adminManager.IsAdmin(session, true);
        var isCurrentlyAdminned = _adminManager.IsAdmin(session) ? _emojis["adminned"] : _emojis["deadminned"];

        if (antag)
            sentText.AppendJoin(' ', _emojis["antagonist"]);

        if (isAdmin)
            sentText.AppendJoin(' ', isCurrentlyAdminned);

        var name = metaData.EntityName + ", " + session.Data.UserName + " ";
        sentText.AppendJoin(' ', $"| {name}");

        return sentText.ToString();
    }

    private bool IsDiscordUserAdmin(CommandReceivedEventArgs args)
    {
        return args.Message.Author is GuildUser guildUser
               && args.Message.Guild != null
               && args.Message.Channel != null
               && guildUser.RoleIds.Any(roleId => _adminRoleIds.Contains(roleId));
    }
}
