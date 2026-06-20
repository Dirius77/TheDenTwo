using Content.Server.Chat.Systems;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.Whitelist;
using Robust.Server.Player;
using Robust.Shared.Random;

namespace Content.Server._DEN.Language.EntitySystems;

public sealed partial class GestaltSystem : EntitySystem
{
    [Dependency] private SharedLanguageSystem _language = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityQuery<GestaltComponent> _gestaltQuery = default!;
    [Dependency] private EntityQuery<GhostHearingComponent> _ghostHearingQuery = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandICChatRecipients);
        SubscribeLocalEvent<GestaltComponent, LanguageRelayedEvent<SpeakAttemptEvent>>(OnSpeakLanguageAttempt);

        SubscribeLocalEvent<GestaltComponent, ComponentStartup>(OnGestaltLanguageStartup);
        SubscribeLocalEvent<GestaltComponent, ExaminedEvent>(OnGestaltLanguageExamined);
    }

    private void OnGestaltLanguageStartup(Entity<GestaltComponent> ent, ref ComponentStartup args)
    {
        _language.OnLanguageUpdated(ent.AsType());
    }

    private void OnGestaltLanguageExamined(Entity<GestaltComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("language-gestalt-language-description"));
    }

    private void OnSpeakLanguageAttempt(Entity<GestaltComponent> entity, ref LanguageRelayedEvent<SpeakAttemptEvent> args)
    {
        var gestalt = entity.Comp;

        // If this gestalt doesn't need a host, then the attempt always succeeds.
        if (!gestalt.RequiresHost)
            return;

        // Gestalt's MUST have a whitelist to differentiate between hosts or we just default to true.
        var foundHost = false;
        if (gestalt.HostWhitelist is { } whitelist)
        {
            // Check all Gestalt Hosts to see if one of them matches our gestalt.
            var hostQuery = EntityQueryEnumerator<GestaltHostComponent>();
            while (hostQuery.MoveNext(out var host, out var _))
            {
                if (!_whitelist.IsWhitelistPass(whitelist, host) || !_mobState.IsAlive(host))
                    continue;

                foundHost = true;
                break;
            }
        }
        else
        {
            foundHost = true;
        }

        if (!foundHost)
        {
            // Explain to the user why they aren't able to speak in the gestalt if we have a message.
            if (gestalt.MissingHostPopups.Count != 0)
            {
                _popupSystem.PopupEntity(Loc.GetString(_random.Pick(gestalt.MissingHostPopups)), args.Owner, args.Owner);
            }
            args.Args.Cancel();
        }
    }

    // Add all players to chat recipients if the language spoken is a gestalt. Other language systems are in charge of
    // filtering out users who shouldn't see the message.
    private void OnExpandICChatRecipients(ExpandICChatRecipientsEvent args)
    {
        if (_language.GetCurrentLanguageEntity(args.Source) is not { } spokenLangEnt)
            return;
        
        // Check to see if the language being spoken is a gestalt.
        if (!_gestaltQuery.TryGetComponent(spokenLangEnt, out var gestalt))
            return;

        var transformSource = Transform(args.Source);
        var sourceCoords = transformSource.Coordinates;

        // Check all the players.
        foreach (var player in _playerManager.Sessions)
        {
            // Don't send it to people in the lobby.
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            var observer = _ghostHearingQuery.HasComp(playerEntity);

            // Observer, or fails the whitelist if it exists.
            if (!(observer || gestalt.ReceiverWhitelist is not { } receiverWhitelist
                  || _whitelist.IsWhitelistPass(receiverWhitelist, playerEntity)))
                continue;

            var transformEntity = Transform(playerEntity);

            float distance = -1;
            if (sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out var dist))
            {
                distance = dist;
            }
            args.Recipients.TryAdd(player, new ChatSystem.ICChatRecipientData(distance, observer));
        }
    }
}
