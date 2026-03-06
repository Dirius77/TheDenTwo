using Content.Shared.Chat;

namespace Content.Shared._DEN.Language.Components;

/// <summary>
///     Makes a language only speakable in certain channels, or prevents it from being spoken in certain channels.
/// </summary>
[RegisterComponent]
public sealed partial class ChatChannelWhitelistComponent : Component
{
    /// <summary>
    ///     The set of chat channels that are valid for this language. Channels are assumed valid if this is not present.
    /// </summary>
    [DataField]
    public List<ChatChannel>? Whitelist;

    /// <summary>
    ///     The set of channels that are not valid for this language. Channels are assumed valid if this is not present.
    /// </summary>
    [DataField]
    public List<ChatChannel>? Blacklist;

    /// <summary>
    ///     Messages to pop up to the user when they try speaking in the wrong channel.
    /// </summary>
    [DataField]
    public List<LocId> FailureMessages = [];
}
