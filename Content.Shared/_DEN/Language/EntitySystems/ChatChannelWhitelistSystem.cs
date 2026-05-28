using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Speech;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Shared._DEN.Language.EntitySystems;

public sealed partial class ChatChannelWhitelistSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChatChannelWhitelistComponent, LanguageRelayedEvent<SpeakLanguageAttemptEvent>>(
            OnSpeakLanguageAttempt);
    }

    private void OnSpeakLanguageAttempt(Entity<ChatChannelWhitelistComponent> ent,
        ref LanguageRelayedEvent<SpeakLanguageAttemptEvent> args)
    {
        var evt = args.Args;

        // If a channel isn't passed to the event then assume speaking is still possible until proven otherwise.
        // The message sending system will always provide a channel, so this is fine and we don't want to interfere
        // with cases where, for example, the person is typing but hasn't selected a channel yet.
        if (evt.Channel is null)
            return;

        if (ent.Comp.Whitelist is { } whitelist && whitelist.Contains(evt.Channel.Value))
            return;

        if (!(ent.Comp.Blacklist is { } blacklist && blacklist.Contains(evt.Channel.Value)))
            return;

        if (ent.Comp.FailureMessages.Count > 0)
        {
            Log.Debug("Doing popup: " + Name(args.Owner));
            _popup.PopupEntity(Loc.GetString(_random.Pick(ent.Comp.FailureMessages)), args.Owner, args.Owner);
        }

        evt.Cancel();
    }
}
