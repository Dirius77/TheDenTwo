using Content.Server._DEN.Language.EntitySystems;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Speech;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioSystem
{
    [Dependency] private LanguageSystem _language = null!;

    public static readonly ProtoId<LanguageWrapperPrototype> RadioWrapper = "RadioWrapper"; // DEN: Languages

    public override void SendRadioMessage(EntityUid messageSource,
        Entity<LanguageComponent> languageEnt,
        ComplexChatMessage message,
        RadioChannelPrototype channel,
        EntityUid radioSource) // DEN Pass Complex messages and language instead.
    {
         // TODO if radios ever garble / modify messages, feedback-prevention needs to be handled better than this.
        if (!_messages.Add(message.OriginalMessage)) // DEN: Languages
            return;

        var evt = new TransformSpeakerNameEvent(messageSource, MetaData(messageSource).EntityName);
        RaiseLocalEvent(messageSource, evt);

        var name = evt.VoiceName;
        name = FormattedMessage.EscapeText(name);

        var language = ProtoMan.Index(languageEnt.Comp.Language); // DEN: Languages

        SpeechVerbPrototype speech;
        if (evt.SpeechVerb != null && ProtoMan.Resolve(evt.SpeechVerb, out var evntProto))
            speech = evntProto;
        else
            speech = _chat.GetComplexSpeechVerb(messageSource, message, language, ChatChannel.Radio); // DEN: Languages

        var verb = Loc.GetString(_random.Pick(speech.SpeechVerbStrings)); // DEN: Languages

        var ev = new RadioReceiveEvent(message, languageEnt, speech, name, verb, messageSource, channel, radioSource); // DEN: Languages

        var sendAttemptEv = new RadioSendAttemptEvent(channel, radioSource);
        RaiseLocalEvent(ref sendAttemptEv);
        RaiseLocalEvent(radioSource, ref sendAttemptEv);
        var canSend = !sendAttemptEv.Cancelled;

        var sourceMapId = Transform(radioSource).MapID;
        var hasActiveServer = HasActiveServer(sourceMapId, channel.ID);
        var sourceServerExempt = _exemptQuery.HasComp(radioSource);

        var radioQuery = EntityQueryEnumerator<ActiveRadioComponent, TransformComponent>();
        while (canSend && radioQuery.MoveNext(out var receiver, out var radio, out var transform))
        {
            if (!radio.ReceiveAllChannels)
            {
                if (!radio.Channels.Contains(channel.ID) || (TryComp<IntercomComponent>(receiver, out var intercom) &&
                                                             !intercom.SupportedChannels.Contains(channel.ID)))
                    continue;
            }

            if (!channel.LongRange && transform.MapID != sourceMapId && !radio.GlobalReceive)
                continue;

            // don't need telecom server for long range channels or handheld radios and intercoms
            var needServer = !channel.LongRange && !sourceServerExempt;
            if (needServer && !hasActiveServer)
                continue;

            // check if message can be sent to specific receiver
            var attemptEv = new RadioReceiveAttemptEvent(channel, radioSource, receiver);
            RaiseLocalEvent(ref attemptEv);
            RaiseLocalEvent(receiver, ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            // send the message
            RaiseLocalEvent(receiver, ref ev);
        }

        // DEN Start: Build wrapped and unwrapped messages for logging and replay.
        var (unwrappedMessage, wrappedMessage) = _chat.BuildComplexMessage(message,
            ProtoMan.Index(RadioWrapper),
            language,
            speech.Bold,
            language.DisplayInChat,
            true,
            name,
            verb,
            channel.LocalizedName,
            channel.Color);

        Log.Debug("Radio: " + wrappedMessage);

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} as {name} on {channel.LocalizedName}: {unwrappedMessage}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} on {channel.LocalizedName}: {unwrappedMessage}");

        _replay.RecordServerMessage(new ChatMessage(
            ChatChannel.Radio,
            unwrappedMessage,
            wrappedMessage,
            NetEntity.Invalid,
            null));
        _messages.Remove(message.OriginalMessage);
        // DEN End
    }

    private void OnIntrinsicReceive(Entity<IntrinsicRadioReceiverComponent> ent, ref RadioReceiveEvent args)
    {
        if (!TryComp(ent.Owner, out ActorComponent? actor))
            return;

        // DEN start: languages. Follow in chat was moved inside the method below because it was ugly and bad.
        _chat.SendComplexMessageToEntity(
            args.RadioSource,
            (ent.Owner, actor),
            args.LanguageEnt,
            args.Message,
            ProtoMan.Index(RadioWrapper),
            ChatChannel.Radio,
            args.Name,
            args.Verb,
            args.Speech.Bold,
            false,
            args.Channel.LocalizedName,
            args.Channel.Color
        );
        // DEN end
    }

    private void OnIntrinsicSpeak(Entity<IntrinsicRadioTransmitterComponent> ent, ref EntitySpokeEvent args)
    {
        if (args.Channel != null && ent.Comp.Channels.Contains(args.Channel.ID))
        {
            SendRadioMessage(ent.Owner, args.LanguageEnt, args.Message, args.Channel, ent.Owner); // DEN: Languages
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }
}
