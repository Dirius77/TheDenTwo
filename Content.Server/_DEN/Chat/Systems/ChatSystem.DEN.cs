using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost.Components;
using Content.Shared.IdentityManagement;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    /// <summary>
    ///     Sends a chat message to the given players in whisper range of the source entity.
    /// </summary>
    private void SendInWhisperClearRange(ChatChannel channel,
        string message,
        string wrappedMessage,
        EntityUid source,
        ChatTransmitRange range,
        NetUserId? author = null)
    {
        foreach (var (session, data) in GetRecipients(source, WhisperClearRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;

            if (session.AttachedEntity is not { Valid: true } targetEntity
                || HasComp<GhostComponent>(targetEntity))
                continue;

            var inRange = _interaction.InRangeUnobstructed(
                source,
                targetEntity,
                WhisperClearRange);

            if (!inRange)
                continue;

            var entHideChat = entRange == MessageRangeCheckResult.HideChat;
            _chatManager.ChatMessageToOne(channel, message, wrappedMessage, source, entHideChat, session.Channel, author: author);
        }

        _replay.RecordServerMessage(new ChatMessage(channel, message, wrappedMessage, GetNetEntity(source), null, MessageRangeHideChatForReplay(range)));
    }

    private void SendEntitySubtle(
        EntityUid source,
        string action,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog = false,
        bool ignoreActionBlocker = false,
        NetUserId? author = null
    )
    {
        if (!_actionBlocker.CanEmote(source) && !ignoreActionBlocker)
            return;

        // get the entity's apparent name (if no override provided).
        var ent = Identity.Entity(source, EntityManager);
        var name = FormattedMessage.EscapeText(nameOverride ?? Name(ent));

        // Emotes use Identity.Name, since it doesn't actually involve your voice at all.
        var wrappedMessage = Loc.GetString("chat-manager-entity-subtle-wrap-message",
            ("entityName", name),
            ("entity", ent),
            ("message", action));

        SendInWhisperClearRange(ChatChannel.Subtle, action, wrappedMessage, source, range, author);

        if (hideLog)
            return;

        if (name != Name(source))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Subtle from {source} as {name}: {action}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Subtle from {source}: {action}");
    }

    private void SendSubtleOOC(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));

        if (_adminManager.IsAdmin(player))
        {
            if (!_adminLoocEnabled)
                return;
        }
        else if (!_loocEnabled)
            return;

        // If crit player LOOC is disabled, don't send the message at all.
        if (!_critLoocEnabled && _mobStateSystem.IsCritical(source))
            return;

        var wrappedMessage = Loc.GetString("chat-manager-entity-subtle-ooc-wrap-message",
            ("entityName", name),
            ("message", FormattedMessage.EscapeText(message)));

        SendInWhisperClearRange(ChatChannel.SubtleOOC,
            message,
            wrappedMessage,
            source,
            hideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal,
            player.UserId);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Subtle OOC from {source}: {message}");
    }
}
