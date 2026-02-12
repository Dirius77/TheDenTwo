using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language;

public interface IKnownLanguagesRelayEvent;

public interface ISpokenLanguageRelayEvent;

public sealed class LanguageRelayedEvent<TEvent>(EntityUid owner, TEvent args) : EntityEventArgs
{
    public TEvent Args = args;
    public EntityUid Owner = owner;
}

public sealed class AttemptUnderstandingEvent(EntityUid sender, ProtoId<LanguagePrototype> language)
    : CancellableEntityEventArgs, ISpokenLanguageRelayEvent
{
    public EntityUid Sender = sender;
    public ProtoId<LanguagePrototype> Language = language;
    public string? MessageOverride;
    public bool HideLanguage;
}
