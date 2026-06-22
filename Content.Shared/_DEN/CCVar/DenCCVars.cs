using Robust.Shared.Configuration;

namespace Content.Shared._DEN.CCVar;

[CVarDefs]
public sealed class DenCCVars
{
    /// <summary>
    /// Stops the server from sending the station broadcast about people cryoing to this client.
    /// </summary>
    public static readonly CVarDef<bool> IgnoreCryoMessage =
        CVarDef.Create("den.ignore_cryo_message", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Discord role IDs that are considered "game admins".
    /// </summary>
    public static readonly CVarDef<string> DiscordAdminRoleIds =
        CVarDef.Create("den.discord_admin_role_ids",
            "1302235169591394305,1302235145889124383,1302235089677320245,1302235039651598386,1302235013986910219,1392313569390886942",
            CVar.SERVERONLY | CVar.ARCHIVE);
}
