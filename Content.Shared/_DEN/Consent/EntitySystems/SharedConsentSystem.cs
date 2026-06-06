using System.Linq;
using Content.Shared._DEN.Consent.Components;
using Content.Shared._DEN.Consent.Managers;
using Content.Shared._DEN.Consent.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Consent.EntitySystems;

public abstract partial class SharedConsentSystem : EntitySystem
{
    [Dependency] protected IConsentManager ConsentManager = null!;

    /// <summary>
    /// Whether a consent on an <see cref="EntityUid"/> is true or not.
    /// </summary>
    /// <param name="uid">The entity to check.</param>
    /// <param name="toggle">The consent to check.</param>
    /// <returns>Whether the consent is true or not.</returns>
    public bool HasConsent(EntityUid uid, ProtoId<ConsentTogglePrototype> toggle)
    {
        var defaultValue = ConsentManager.GetDefaultValue(toggle);

        if (TryComp<ConsentComponent>(uid, out var consent)
            && consent.ConsentToggles.Contains(toggle))
            return !defaultValue;

        return defaultValue;
    }
}

public record struct UserConsentInfo(ProtoId<ConsentTogglePrototype> ToggleId, bool ToggleValue)
{
    public static List<UserConsentInfo> FromDictionary(Dictionary<ProtoId<ConsentTogglePrototype>, bool> consentInfo)
    {
        return consentInfo
            .Select(info => new UserConsentInfo(info.Key, info.Value))
            .ToList();
    }

    public static Dictionary<ProtoId<ConsentTogglePrototype>, bool> ToDictionary(List<UserConsentInfo> consentInfo)
    {
        return consentInfo.ToDictionary(info => info.ToggleId, info => info.ToggleValue);
    }
}
