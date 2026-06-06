using Content.Shared._DEN.Consent.Components;
using Content.Shared._DEN.Consent.Prototypes;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._DEN.Consent.Managers;

namespace Content.Shared._DEN.Consent.Events;

/// <summary>
/// Used to inform same-side entity systems about updated consents, for whatever reason.
/// </summary>
/// <remarks>
/// This will only run if the user in question has a valid AttachedEntity.
/// <see cref="IConsentManager"/> contains an action that always runs on update.
/// </remarks>
public sealed class ConsentUpdatedEvent(EntityUid uid) : EntityEventArgs
{
    public EntityUid Entity { get; } = uid;
}

/// <summary>
/// Used to update consent settings on either the client or the server.
/// </summary>
public sealed class MsgUpdateConsent : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public List<ProtoId<ConsentTogglePrototype>> NotDefaultConsents = [];

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var length = buffer.ReadVariableInt32();

        NotDefaultConsents.Clear();

        for (var i = 0; i < length; i++)
        {
            var updatedConsent = (ProtoId<ConsentTogglePrototype>) buffer.ReadString();
            NotDefaultConsents.Add(updatedConsent);
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(NotDefaultConsents.Count);

        foreach (var consentId in NotDefaultConsents)
        {
            buffer.Write(consentId);
        }
    }
}
