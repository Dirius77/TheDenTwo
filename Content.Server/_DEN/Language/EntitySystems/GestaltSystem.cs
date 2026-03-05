using Content.Server.Chat.Systems;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared.Ghost;
using Content.Shared.Whitelist;
using Robust.Server.Player;

namespace Content.Server._DEN.Language.EntitySystems;

public sealed partial class GestaltSystem : EntitySystem
{
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private EntityQuery<GestaltComponent> _gestaltQuery;

    public override void Initialize()
    {
        _gestaltQuery = GetEntityQuery<GestaltComponent>();

        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandICChatRecipients);
    }

    private void OnExpandICChatRecipients(ExpandICChatRecipientsEvent args)
    {
        if (_language.GetCurrentLanguageEntity(args.Source) is not { } spokenLangEnt)
            return;

        if (!_gestaltQuery.TryGetComponent(spokenLangEnt, out var gestalt))
            return;

        if (gestalt.RequiresHost)
        {
            var foundHost = false;
            if (gestalt.HostWhitelist is { } whitelist)
            {
                var hostQuery = EntityQueryEnumerator<GestaltHostComponent>();
                while (hostQuery.MoveNext(out var host, out var _))
                {
                    if (!_whitelist.IsWhitelistPass(whitelist, host))
                        continue;

                    foundHost = true;
                    break;
                }
            }
            else
            {
                foundHost = true;
                Log.Warning("Gestalt wants a host but has no host whitelist, defaulting to success.");
            }

            if (!foundHost)
                return;
        }

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
