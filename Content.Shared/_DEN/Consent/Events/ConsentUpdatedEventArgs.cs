using Content.Shared._DEN.Consent.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Consent.Events;

public sealed class ConsentUpdatedEventArgs(
    NetUserId userId,
    ProtoId<ConsentTogglePrototype> toggleId,
    bool newValue) : EventArgs
{
    /// <summary>
    /// The user ID of the person having their consent updated.
    /// </summary>
    public readonly NetUserId UserId = userId;

    /// <summary>
    /// The <see cref="ConsentTogglePrototype"/> ID of the toggle being changed.
    /// </summary>
    public readonly ProtoId<ConsentTogglePrototype> ToggleId = toggleId;
    /// <summary>
    /// The new value of the toggle.
    /// </summary>
    public readonly bool ToggleValue = newValue;
}
