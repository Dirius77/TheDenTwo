using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Language;

public interface IKnownLanguagesRelayEvent;

public interface ISpokenLanguageRelayEvent;

public sealed class LanguageRelayedEvent<TEvent>(EntityUid owner, TEvent args) : EntityEventArgs
{
    public TEvent Args = args;
    public EntityUid Owner = owner;
}

/// <summary>
///     Called on an entity when it is attempting to understand a particular language.
/// </summary>
/// <param name="sender">The entity sending the message</param>
/// <param name="language">The language being spoken</param>
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
    string verb,
    ChatChannel chatChannel)
    : EntityEventArgs, ISpokenLanguageRelayEvent
{
    public EntityUid Sender = sender;
    public EntityUid Listener = listener;
    public ComplexChatMessage Message = message;
    public LanguagePrototype Language = language;
    public LanguageFluencyPrototype Understanding = understanding;
    public string Name = name;
    public string Verb = verb;
    public ChatChannel Channel = chatChannel;
}

public sealed class LanguageAddedToCommunicatorEvent(Entity<LanguageComponent> language) : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.All;

    public Entity<LanguageComponent> Language = language;
}

public sealed class LanguageRemovedFromCommunicatorEvent(Entity<LanguageComponent> language) : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.All;

    public Entity<LanguageComponent> Language = language;
}

public sealed class TransformLanguageEvent(EntityUid sender, ComplexChatMessage message) : EntityEventArgs, ISpokenLanguageRelayEvent
{
    public EntityUid Sender = sender;
    public ComplexChatMessage Message = message;
}

[Serializable, NetSerializable]
public sealed class RequestSetSpokenLanguageEvent : EntityEventArgs
{
    public readonly NetEntity LanguageEntity;

    public RequestSetSpokenLanguageEvent(NetEntity languageEntity)
    {
        LanguageEntity = languageEntity;
    }
}
