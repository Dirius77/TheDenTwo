using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Radio;

namespace Content.Shared.Chat;

/// <summary>
/// Raised on an entity when it speaks with a language.
/// </summary>
public sealed class EntitySpokeLanguageEvent : EntityEventArgs
{
    public readonly EntityUid Source;
    public readonly Entity<LanguageComponent> LanguageEnt;
    public readonly ComplexChatMessage Message;
    public readonly string Verb;
    public readonly ChatChannel ChatChannel;

    /// <summary>
    /// If the entity was trying to speak into a radio, this was the channel they were trying to access. If a radio
    /// message gets sent on this channel, this should be set to null to prevent duplicate messages.
    /// </summary>
    public RadioChannelPrototype? RadioChannel;

    /// <summary>
    /// Event called on an entity when it speaks with a language.
    /// </summary>
    /// <param name="source">The entity speaking.</param>
    /// <param name="languageEnt">The language entity being spoken.</param>
    /// <param name="message">The message being spoken.</param>
    /// <param name="radioChannel">The radio channel being spoken on, if there is one.</param>
    /// <param name="verb">The verb that will be used for this message, if one is needed.</param>
    /// <param name="chatChannel">The ChatChannel that is being spoken on.</param>
    public EntitySpokeLanguageEvent(EntityUid source, Entity<LanguageComponent> languageEnt, ComplexChatMessage message, RadioChannelPrototype? radioChannel, string verb, ChatChannel chatChannel)
    {
        Source = source;
        Message = message;
        LanguageEnt = languageEnt;
        RadioChannel = radioChannel;
        Verb = verb;
        ChatChannel = chatChannel;
    }
}
