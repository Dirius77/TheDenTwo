using Content.Server.Chat.Systems;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared._DEN.Speech;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Server.Player;
using Robust.Shared.Random;

namespace Content.Server._DEN.Language.EntitySystems;

public sealed partial class GestaltSystem : EntitySystem
{
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private EntityQuery<GestaltComponent> _gestaltQuery;

    public override void Initialize()
    {
        _gestaltQuery = GetEntityQuery<GestaltComponent>();

        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandICChatRecipients);
        SubscribeLocalEvent<GestaltComponent, LanguageRelayedEvent<SpeakLanguageAttemptEvent>>(OnSpeakLanguageAttempt);

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

    private void OnSpeakLanguageAttempt(Entity<GestaltComponent> entity, ref LanguageRelayedEvent<SpeakLanguageAttemptEvent> args)
    {
        var gestalt = entity.Comp;

        if (!gestalt.RequiresHost)
            return;

        var foundHost = false;
        if (gestalt.HostWhitelist is { } whitelist)
        {
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
            if (gestalt.MissingHostPopups.Count != 0)
            {
                _popupSystem.PopupEntity(Loc.GetString(_random.Pick(gestalt.MissingHostPopups)), args.Owner, args.Owner);
            }
            args.Args.Cancel();
        }
    }

    private void OnExpandICChatRecipients(ExpandICChatRecipientsEvent args)
    {
        if (_language.GetCurrentLanguageEntity(args.Source) is not { } spokenLangEnt)
            return;

        if (!_gestaltQuery.TryGetComponent(spokenLangEnt, out var gestalt))
            return;

        var ghostHearing = GetEntityQuery<GhostHearingComponent>();
        var xforms = GetEntityQuery<TransformComponent>();

        var transformSource = xforms.GetComponent(args.Source);
        var sourceCoords = transformSource.Coordinates;

        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            var observer = ghostHearing.HasComp(playerEntity);

            // Observer, or fails the whitelist if it exists.
            if (!(observer || gestalt.ReceiverWhitelist is not { } receiverWhitelist
                  || _whitelist.IsWhitelistPass(receiverWhitelist, playerEntity)))
                continue;

            var transformEntity = xforms.GetComponent(playerEntity);

            float distance = -1;
            if (sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out var dist))
            {
                distance = dist;
            }
            args.Recipients.TryAdd(player, new ChatSystem.ICChatRecipientData(distance, observer));
        }
    }
}
