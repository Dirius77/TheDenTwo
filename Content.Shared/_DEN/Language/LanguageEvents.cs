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
///
/// </summary>
/// <param name="sender"></param>
/// <param name="language"></param>
public sealed class AttemptUnderstandingEvent(EntityUid sender, LanguagePrototype language)
    : HandledEntityEventArgs, IKnownLanguagesRelayEvent
{
    public EntityUid Sender = sender;
    public LanguagePrototype Language = language;
    public Entity<LanguageComponent>? Understanding;
    public bool HideLanguage = false;
    public bool HideMessage = false;
}

public sealed class LanguageModifyMessageEvent(
    EntityUid sender,
    EntityUid listener,
    ComplexChatMessage message,
    LanguagePrototype language,
    LanguageFluencyPrototype understanding,
    string name,
    bool isWhisper)
    : EntityEventArgs, ISpokenLanguageRelayEvent
{
    public EntityUid Sender = sender;
    public EntityUid Listener = listener;
    public ComplexChatMessage Message = message;
    public LanguagePrototype Language = language;
    public LanguageFluencyPrototype Understanding = understanding;
    public string Name = name;
    public bool IsWhisper = isWhisper;
}
