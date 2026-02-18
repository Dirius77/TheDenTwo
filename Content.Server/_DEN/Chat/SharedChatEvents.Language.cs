using Content.Shared._DEN.Language;
using Content.Shared.Radio;

namespace Content.Shared.Chat;

public sealed class EntitySpokeLanguageEvent : EntityEventArgs
{
    public readonly EntityUid Source;
    public readonly ComplexChatMessage Message;
    public readonly LanguagePrototype Language;
    public readonly string Verb;
    public readonly bool Whisper;

    /// <summary>
    /// If the entity was trying to speak into a radio, this was the channel they were trying to access. If a radio
    /// message gets sent on this channel, this should be set to null to prevent duplicate messages.
    /// </summary>
    public RadioChannelPrototype? Channel;

    public EntitySpokeLanguageEvent(EntityUid source, ComplexChatMessage message, LanguagePrototype language, RadioChannelPrototype? channel, string verb, bool whisper = false)
    {
        Source = source;
        Message = message;
        Language = language;
        Channel = channel;
        Verb = verb;
        Whisper = whisper;
    }
}
