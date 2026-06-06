using System.Linq;
using System.Threading;
using Content.Server.Database;
using Content.Shared._DEN.Consent.Components;
using Content.Shared._DEN.Consent.EntitySystems;
using Content.Shared._DEN.Consent.Events;
using Content.Shared._DEN.Consent.Managers;
using Content.Shared._DEN.Consent.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._DEN.Consent.EntitySystems;

/// <summary>
/// This handles server-sided consent information.
/// </summary>
public sealed partial class ConsentSystem : SharedConsentSystem
{
    [Dependency] private IConsentManager _consentManager = null!;
    [Dependency] private IPlayerManager _playerManager = null!;
    [Dependency] private IPrototypeManager _protoManager = null!;
    [Dependency] private IServerDbManager _dbManager = null!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerAttachedEvent>(OnMindAdded);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnMindRemoved);

        _consentManager.OnConsentUpdated += UpdateSavedConsent;
        _consentManager.OnConsentRequestingData += LoadData;
    }

    private async void UpdateSavedConsent(ConsentUpdatedEventArgs args)
    {
        if (!_playerManager.TryGetSessionById(args.UserId, out var session))
            return;

        await _dbManager.SetConsentData(session.UserId, args.ToggleId, args.ToggleValue);
        OnConsentUpdated(args.UserId);
    }

    private async void LoadData(NetUserId userId)
    {
        if (!_playerManager.TryGetSessionById(userId, out _))
            return;

        var data = await _dbManager.GetConsentData(userId);
        var defaultData = _consentManager.GetDefaultToggles();
        var toggleDictionary = UserConsentInfo.ToDictionary(defaultData);

        foreach (var consentData in data)
        {
            // Don't add prototype IDs that don't exist.
            if (!_protoManager.TryIndex<ConsentTogglePrototype>(consentData.ConsentId, out _))
                continue;

            toggleDictionary[consentData.ConsentId] = consentData.ConsentValue;
        }

        var newConsentToggles = UserConsentInfo.FromDictionary(toggleDictionary);
        _consentManager.SetConsentToggles(userId, newConsentToggles);
    }

    private async void OnConsentUpdated(NetUserId userId)
    {
        if (!_playerManager.TryGetSessionById(userId, out var session)
            || session.AttachedEntity is not { Valid: true } attachedEntity)
            return;

        var consentToggles = ConsentManager.GetConsentToggles(userId);
        var consentComponent = EnsureComp<ConsentComponent>(attachedEntity);

        if (consentComponent.ConsentToggles == consentToggles)
            return;

        var ent = (attachedEntity, consentComponent);
        consentComponent.ConsentToggles = consentToggles;

        Dirty<ConsentComponent>(ent);
        RaiseLocalEvent(new ConsentUpdatedEvent(attachedEntity));
    }

    private void OnMindAdded(PlayerAttachedEvent ev)
    {
        BuildConsentComponent(ev);
    }

    private void OnMindRemoved(PlayerDetachedEvent ev)
    {
        RemComp<ConsentComponent>(ev.Entity);
    }

    private void BuildConsentComponent(PlayerAttachedEvent ev)
    {
        var userId = ev.Player.UserId;

        var consentToggles = ConsentManager.GetConsentToggles(userId);
        var consentComponent = EnsureComp<ConsentComponent>(ev.Entity);
        consentComponent.ConsentToggles = consentToggles;

        Dirty<ConsentComponent>((ev.Entity, consentComponent));
    }
}
