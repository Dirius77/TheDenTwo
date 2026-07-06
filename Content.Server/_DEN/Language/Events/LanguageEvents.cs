using Content.Shared.Chat;
using Content.Shared.Radio;

namespace Content.Server._DEN.Language.Events;

/// <summary>
/// Raised on a language entity when it is used to speak.
/// </summary>
public sealed class LanguageSpokenWithEvent : EntityEventArgs
{
    /// <summary>
    /// The Entity doing the speaking.
    /// </summary>
    public readonly EntityUid Source;
    
    /// <summary>
    /// The message that was spoken.
    /// </summary>
    public readonly ComplexChatMessage Message;
    
    /// <summary>
    /// The chat channel spoken in.
    /// </summary>
    public readonly ChatChannel ChatChannel;
    
    /// <summary>
    /// The radio channel spoken in, if any.
    /// </summary>
    public readonly RadioChannelPrototype? Channel;

    /// <summary>
    /// Event called on a language entity when it is used to speak.
    /// </summary>
    /// <param name="source">The Entity doing the speaking.</param>
    /// <param name="message">The message that was spoken.</param>
    /// <param name="channel">The chat channel spoken in.</param>
    /// <param name="chatChannel">The radio channel spoken in, if any.</param>
    public LanguageSpokenWithEvent(EntityUid source, ComplexChatMessage message, RadioChannelPrototype? channel,
        ChatChannel chatChannel)
    {
        Source = source;
        Message = message;
        Channel = channel;
        ChatChannel = chatChannel;
    }
}