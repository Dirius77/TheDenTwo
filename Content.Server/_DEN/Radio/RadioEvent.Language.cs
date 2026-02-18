using Content.Shared._DEN.Language;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Content.Shared.Speech;

namespace Content.Server.Radio;

[ByRefEvent]
public readonly record struct RadioReceiveLanguageEvent(ComplexChatMessage Message, LanguagePrototype Language, SpeechVerbPrototype Speech, string Name, string Verb, EntityUid MessageSource, RadioChannelPrototype Channel, EntityUid RadioSource);

[ByRefEvent]
public readonly record struct HeadsetRadioReceiveLanguageRelayEvent(RadioReceiveLanguageEvent RelayedEvent);
