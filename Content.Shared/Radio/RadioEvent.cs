using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Content.Shared.Speech;

namespace Content.Shared.Radio;

/// <summary>
/// Event raised when a radio message is received.
/// </summary>
[ByRefEvent]
public readonly record struct RadioReceiveEvent(ComplexChatMessage Message, Entity<LanguageComponent> LanguageEnt, SpeechVerbPrototype Speech, string Name, string Verb, EntityUid MessageSource, RadioChannelPrototype Channel, EntityUid RadioSource); // DEN: Use ComplexChatMessage system instead of just passing network message structs around.

/// <summary>
/// Event raised on the parent entity of a headset radio when a radio message is received.
/// </summary>
[ByRefEvent]
public readonly record struct HeadsetRadioReceiveRelayEvent(RadioReceiveEvent RelayedEvent);

/// <summary>
/// Use this event to cancel sending message per receiver.
/// </summary>
[ByRefEvent]
public record struct RadioReceiveAttemptEvent(RadioChannelPrototype Channel, EntityUid RadioSource, EntityUid RadioReceiver)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public readonly EntityUid RadioReceiver = RadioReceiver;
    public bool Cancelled = false;
}

/// <summary>
/// Use this event to cancel sending message to every receiver.
/// </summary>
[ByRefEvent]
public record struct RadioSendAttemptEvent(RadioChannelPrototype Channel, EntityUid RadioSource)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public bool Cancelled = false;
}
