using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Telephone;

namespace Content.Server.Telephone;

/// <summary>
/// Raised when a chat message is sent by a telephone to another
/// </summary>
[ByRefEvent]
public readonly record struct TelephoneMessageLanguageSentEvent(
    ComplexChatMessage Message,
    Entity<LanguageComponent> LanguageEnt,
    EntityUid MessageSource);

/// <summary>
/// Raised when a chat message is received by a telephone from another
/// </summary>
[ByRefEvent]
public readonly record struct TelephoneMessageLanguageReceivedEvent(
    ComplexChatMessage Message,
    Entity<LanguageComponent> LanguageEnt,
    string Verb,
    string Name,
    EntityUid MessageSource,
    Entity<TelephoneComponent> TelephoneSource);
