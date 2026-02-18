using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;

namespace Content.Shared._DEN.Language;

public interface IKnownLanguagesRelayEvent;

public interface ISpokenLanguageRelayEvent;

public sealed class LanguageRelayedEvent<TEvent>(EntityUid owner, TEvent args) : EntityEventArgs
{
    public TEvent Args = args;
    public EntityUid Owner = owner;
}

/// <summary>
///     Tries to retrieve what a listener's view of a language should look like. If the entity has a language entity
///     that allows them to understand the language, then Understanding should be set to this entity. However it is
///     also possible for something to override the understanding, in which case Understanding may be set to null.
///     MessageOverride should always be displayed to the understanding entity regardless of the state of Understanding.
///     Unhandled means that nothing interacted with the language, assume that they did not understand it. Handlers
///     may also force a language name to be hidden. This is useful for if, for example, someone is completely incapable
///     of interpreting a language that would otherwise be obvious (IE, being deaf listening to a language, or blind with sign).
///
///     The entity that gets returned as the one in charge of Understanding will have the LanguageModifyMessageEvent
///     called on it.
/// </summary>
/// <param name="sender"></param>
/// <param name="language"></param>
public sealed class AttemptUnderstandingEvent(EntityUid sender, LanguagePrototype language)
    : HandledEntityEventArgs, IKnownLanguagesRelayEvent
{
    public EntityUid Sender = sender;
    public LanguagePrototype Language = language;
    public Entity<LanguageComponent>? Understanding;
    public string? MessageOverride;
    public bool HideLanguage = false;
    public bool HideMessage = false;
}

public sealed class LanguageModifyMessageEvent(
    EntityUid sender,
    EntityUid listener,
    ComplexChatMessage message,
    LanguagePrototype language,
    bool isWhisper)
    : EntityEventArgs
{
    public EntityUid Sender = sender;
    public EntityUid Listener = listener;
    public ComplexChatMessage Message = message;
    public LanguagePrototype Language = language;
    public bool IsWhisper = isWhisper;
}
