using Content.Shared._DEN.Consent.Components;
using Content.Shared._DEN.Consent.EntitySystems;
using Content.Shared._DEN.Consent.Events;
using Content.Shared._DEN.Consent.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Consent.Managers;

/// <summary>
/// Handle player consent information
/// </summary>
public interface IConsentManager
{
    event Action<ConsentUpdatedEventArgs>? OnConsentUpdated;
    event Action<NetUserId>? OnConsentRequestingData;

    void Initialize();
    /// <summary>
    /// Sets the value of a consent toggle.
    /// </summary>
    /// <param name="userId">The user ID of the person who should have their toggle value changed/</param>
    /// <param name="toggle">The toggle being changed.</param>
    /// <param name="newValue">The new value being </param>
    void SetConsentToggle(NetUserId userId, ProtoId<ConsentTogglePrototype> toggle, bool newValue);
    /// <summary>
    /// Sets the value of every consent, overriding all consents in the cache.
    /// </summary>
    /// <remarks>Used for initial data loading, such as with the database.</remarks>
    /// <param name="userId"></param>
    /// <param name="toggles"></param>
    void SetConsentToggles(NetUserId userId, List<UserConsentInfo> toggles);
    /// <summary>
    /// Gets the list of consent toggles that are different to default values for use in <see cref="ConsentComponent"/>.
    /// </summary>
    /// <param name="userId">The user to retrieve the consent toggles from.</param>
    /// <returns></returns>
    List<ProtoId<ConsentTogglePrototype>> GetConsentToggles(NetUserId userId);
    /// <summary>
    /// Get the default consent values as stored in <see cref="ConsentTogglePrototype"/>.
    /// </summary>
    /// <returns>The cached default value of every consent toggle.</returns>
    List<UserConsentInfo> GetDefaultToggles();
    /// <summary>
    /// Gets the cached default value from <see cref="ConsentTogglePrototype"/>
    /// </summary>
    /// <param name="toggle">The toggle to return the cached default value of.</param>
    /// <returns></returns>
    bool GetDefaultValue(ProtoId<ConsentTogglePrototype> toggle);
}

