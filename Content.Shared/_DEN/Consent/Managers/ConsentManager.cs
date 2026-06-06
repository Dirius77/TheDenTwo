using System.Linq;
using Content.Shared._DEN.Consent.EntitySystems;
using Content.Shared._DEN.Consent.Events;
using Content.Shared._DEN.Consent.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Consent.Managers;

/// <summary>
/// Used to store consent information.
/// Only stores values that are not the default value of the toggle.
/// </summary>
public sealed partial class ConsentManager : IConsentManager
{
    [Dependency] private ISharedPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;

    [ViewVariables]
    private Dictionary<NetUserId, List<UserConsentInfo>> InternalConsents { get; } = new();
    private Dictionary<ProtoId<ConsentTogglePrototype>, bool> DefaultToggleValues { get; } = new();

    public event Action<ConsentUpdatedEventArgs>? OnConsentUpdated;
    public event Action<NetUserId>? OnConsentRequestingData;

    public void Initialize()
    {
        _protoManager.PrototypesReloaded += OnPrototypesReloaded;
        _playerManager.PlayerStatusChanged += (_, args) => OnPlayerStatusChanged(args);
        CacheDefaultToggleValues();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ConsentTogglePrototype>())
            CacheDefaultToggleValues();
    }

    private void OnPlayerStatusChanged(SessionStatusEventArgs args)
    {
        if (InternalConsents.TryGetValue(args.Session.UserId, out _))
            return;

        OnConsentRequestingData?.Invoke(args.Session.UserId);
    }

    private void CacheDefaultToggleValues()
    {
        DefaultToggleValues.Clear();

        foreach (var toggle in _protoManager.EnumeratePrototypes<ConsentTogglePrototype>())
        {
            DefaultToggleValues[toggle.ID] = toggle.DefaultValue;
        }
    }

    public void SetConsentToggle(NetUserId userId, ProtoId<ConsentTogglePrototype> toggleId, bool newValue)
    {
        if (!InternalConsents.ContainsKey(userId))
            InternalConsents[userId] = GetDefaultToggles();

        var toggles = GetConsentTogglesExcept(userId, toggleId);
        var toggle = new UserConsentInfo(toggleId, newValue);

        if (newValue != DefaultToggleValues[toggleId])
            toggles.Add(toggle);

        InternalConsents[userId] = toggles;

        var updatedEvent = new ConsentUpdatedEventArgs(userId, toggle.ToggleId, toggle.ToggleValue);
        OnConsentUpdated?.Invoke(updatedEvent);
    }

    public void SetConsentToggles(NetUserId userId, List<UserConsentInfo> toggles)
    {
        InternalConsents[userId] = toggles;
    }

    public List<ProtoId<ConsentTogglePrototype>> GetConsentToggles(NetUserId userId)
    {
        var exists = InternalConsents.TryGetValue(userId, out var consentToggles);

        if (!exists || consentToggles == null)
            return [];

        var consentIds = new List<ProtoId<ConsentTogglePrototype>>();

        foreach (var toggle in consentToggles)
        {
            if (toggle.ToggleValue == DefaultToggleValues[toggle.ToggleId])
                continue;

            consentIds.Add(toggle.ToggleId);
        }

        return consentIds;
    }

    public List<UserConsentInfo> GetDefaultToggles()
    {
        return DefaultToggleValues
            .Select(pair => new UserConsentInfo(pair.Key, pair.Value))
            .ToList();
    }

    private List<UserConsentInfo> GetConsentTogglesExcept(NetUserId userId, ProtoId<ConsentTogglePrototype> toggleId)
    {
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (!InternalConsents.TryGetValue(userId, out var consentToggles))
            consentToggles = GetDefaultToggles();

        var consentIds = new List<UserConsentInfo>();

        foreach (var toggle in consentToggles)
        {
            if (toggle.ToggleId == toggleId)
                continue;

            consentIds.Add(toggle);
        }

        return consentIds;
    }

    public bool GetDefaultValue(ProtoId<ConsentTogglePrototype> toggle)
    {
        return DefaultToggleValues[toggle];
    }
}
