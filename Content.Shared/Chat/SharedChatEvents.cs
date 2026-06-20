using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat;

/// <summary>
/// This event should be sent everytime an entity talks (Radio, local chat, etc...).
/// The event is sent to both the entity itself, and all clothing (For stuff like voice masks).
/// </summary>
public sealed class TransformSpeakerNameEvent : EntityEventArgs, IInventoryRelayEvent, ISpokenLanguageRelayEvent // DEN: Languages
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public EntityUid Sender;
    public string VoiceName;
    public ProtoId<SpeechVerbPrototype>? SpeechVerb;

    public TransformSpeakerNameEvent(EntityUid sender, string name)
    {
        Sender = sender;
        VoiceName = name;
        SpeechVerb = null;
    }
}

/// <summary>
/// Raised broadcast in order to transform speech.transmit
/// </summary>
public sealed class TransformSpeechEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public EntityUid Sender;
    public string Message;

    public TransformSpeechEvent(EntityUid sender, string message)
    {
        Sender = sender;
        Message = message;
    }
}

public sealed class CheckIgnoreSpeechBlockerEvent : EntityEventArgs
{
    public EntityUid Sender;
    public bool IgnoreBlocker;

    public CheckIgnoreSpeechBlockerEvent(EntityUid sender, bool ignoreBlocker)
    {
        Sender = sender;
        IgnoreBlocker = ignoreBlocker;
    }
}

/// <summary>
/// Raised on an entity when it speaks, either through 'say' or 'whisper'.
/// </summary>
// DEN Start: Convert to languages and ComplexChatMessage. Also pass the ChatChannel so systems can use it.
public sealed class EntitySpokeEvent : EntityEventArgs
{
    public readonly EntityUid Source;
    public readonly Entity<LanguageComponent> LanguageEnt; // DEN: Languages
    public readonly ComplexChatMessage Message;
    public readonly string Verb;
    public readonly ChatChannel ChatChannel;

    /// <summary>
    /// If the entity was trying to speak into a radio, this was the channel they were trying to access. If a radio
    /// message gets sent on this channel, this should be set to null to prevent duplicate messages.
    /// </summary>
    public RadioChannelPrototype? Channel;

    /// <summary>
    /// Event called on an entity when it speaks with a language.
    /// </summary>
    /// <param name="source">The entity speaking.</param>
    /// <param name="languageEnt">The language entity being spoken.</param>
    /// <param name="message">The message being spoken.</param>
    /// <param name="channel">The radio channel being spoken on, if there is one.</param>
    /// <param name="verb">The verb that will be used for this message, if one is needed.</param>
    /// <param name="chatChannel">The ChatChannel that is being spoken on.</param>
    public EntitySpokeEvent(EntityUid source, Entity<LanguageComponent> languageEnt, ComplexChatMessage message, RadioChannelPrototype? channel, string verb, ChatChannel chatChannel)
    {
        Source = source;
        Message = message;
        LanguageEnt = languageEnt;
        Channel = channel;
        Verb = verb;
        ChatChannel = chatChannel;
    }
}
// DEN End
