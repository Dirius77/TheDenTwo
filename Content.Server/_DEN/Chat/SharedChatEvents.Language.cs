using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Radio;

namespace Content.Shared.Chat;

public sealed class EntitySpokeLanguageEvent : EntityEventArgs
{
    public readonly EntityUid Source;
    public readonly Entity<LanguageComponent?> LanguageEnt;
    public readonly ComplexChatMessage Message;
    public readonly string Verb;
    public readonly ChatChannel ChatChannel;

    /// <summary>
    /// If the entity was trying to speak into a radio, this was the channel they were trying to access. If a radio
    /// message gets sent on this channel, this should be set to null to prevent duplicate messages.
    /// </summary>
    public RadioChannelPrototype? RadioChannel;

    public EntitySpokeLanguageEvent(EntityUid source, Entity<LanguageComponent?> languageEnt, ComplexChatMessage message, RadioChannelPrototype? radioChannel, string verb, ChatChannel chatChannel)
    {
        Source = source;
        Message = message;
        LanguageEnt = languageEnt;
        RadioChannel = radioChannel;
        Verb = verb;
        ChatChannel = chatChannel;
    }
}
