using Content.Server._DEN.Language.EntitySystems;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Speech;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public sealed partial class RadioSystem : EntitySystem
{
    [Dependency] private INetManager _netMan = default!;
    [Dependency] private IReplayRecordingManager _replay = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private LanguageSystem _language = default!; // DEN: Languages
    [Dependency] private EntityQuery<TelecomExemptComponent> _exemptQuery = default!;
    [Dependency] private EntityQuery<RadioTransmittableComponent> _radioLang = default!; // DEN: Languages

    public static readonly ProtoId<LanguageWrapperPrototype> RadioWrapper = "RadioWrapper"; // DEN: Languages

    // set used to prevent radio feedback loops.
    private readonly HashSet<string> _messages = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveEvent>(OnIntrinsicReceive);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EntitySpokeEvent>(OnIntrinsicSpeak);
    }
    
    private void OnIntrinsicSpeak(EntityUid uid, IntrinsicRadioTransmitterComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null && component.Channels.Contains(args.Channel.ID))
        {
            SendRadioMessage(uid, args.LanguageEnt, args.Message, args.Channel, uid); // DEN: Languages
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }
    
    private void OnIntrinsicReceive(EntityUid uid, IntrinsicRadioReceiverComponent component, ref RadioReceiveEvent args)
    {
        // DEN Start: Languages
        if (HasComp<ActorComponent>(uid))
        {
            _chat.SendComplexMessageToEntity(
                args.RadioSource,
                uid,
                args.LanguageEnt,
                args.Message,
                _prototype.Index(RadioWrapper),
                ChatChannel.Radio,
                args.Name,
                args.Verb,
                args.Speech.Bold,
                false,
                args.Channel.LocalizedName,
                args.Channel.Color
            );
        }
        // DEN End
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    public void SendRadioMessage(EntityUid messageSource, string message, ProtoId<RadioChannelPrototype> channel, EntityUid radioSource, bool escapeMarkup = true)
    {
        // DEN Start: Complex messages and languages
        var complex = new ComplexChatMessage(message, "\"", false, true, false, escapeMarkup);
        var languageEnt = _language.GetCurrentLanguageEntity(messageSource, true);
        if (languageEnt is null)
        {
            Log.Warning("Default language entity is null! Unable to send message.");
            return;
        }
        // DEN End
        
        SendRadioMessage(messageSource, languageEnt.Value, complex, _prototype.Index(channel), radioSource); // DEN: Pass Languages and complex
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    /// <param name="messageSource">Entity that spoke the message</param>
    /// <param name="radioSource">Entity that picked up the message and will send it, e.g. headset</param>
    public void SendRadioMessage(EntityUid messageSource,
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

        var language = _prototype.Index(languageEnt.Comp.Language); // DEN: Languages
        
        SpeechVerbPrototype speech;
        if (evt.SpeechVerb != null && _prototype.Resolve(evt.SpeechVerb, out var evntProto))
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
            _prototype.Index(RadioWrapper),
            language,
            speech.Bold,
            language.DisplayInChat,
            true,
            name,
            verb,
            channel.LocalizedName,
            channel.Color);
        
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

    /// <inheritdoc cref="TelecomServerComponent"/>
    private bool HasActiveServer(MapId mapId, string channelId)
    {
        var servers = EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == mapId &&
                power.Powered &&
                keys.Channels.Contains(channelId))
            {
                return true;
            }
        }
        return false;
    }
}
