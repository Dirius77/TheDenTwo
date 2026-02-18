using Content.Server._DEN.Language.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Construction.Steps;
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
    [Dependency] private readonly LanguageSystem _language = default!;

    public static readonly ChatSystem.WrapperSet RadioWrapper = new()
    {
        DialogWrapper = "chat-language-entity-speak-wrap-dialog",
        EmoteWrapper = "chat-language-entity-speak-wrap-emote",
        LanguageWrapper = "chat-language-entity-speak-wrap-language",
        PrefixWrapper = "chat-language-entity-radio-wrap-prefix",
        MessageWrapper = "chat-language-entity-radio-wrap-message",
    };

    private void InitializeLanguage()
    {
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveLanguageEvent>(OnIntrinsicLanguageReceive);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EntitySpokeLanguageEvent>(OnIntrinsicSpeakLanguage);
    }

    private void OnIntrinsicLanguageReceive(EntityUid uid,
        IntrinsicRadioReceiverComponent component,
        ref RadioReceiveLanguageEvent args)
    {
        if (TryComp(uid, out ActorComponent? actor))
        {
            _chat.SendComplexMessageToEntity(
                args.RadioSource,
                uid,
                args.Message,
                args.Language,
                RadioWrapper,
                ChatChannel.Radio,
                args.Name,
                args.Verb,
                args.Speech.Bold,
                false,
                false,
                args.Channel.LocalizedName,
                args.Channel.Color
                );

            // TODO: Put all this shit in one place. (See ChatSystem.Language)
            var understandEv = new AttemptUnderstandingEvent(uid, args.Language);
            RaiseLocalEvent(uid, understandEv);

            if (!understandEv.Handled)
                return;

            var hideLanguage = !args.Language.DisplayInChat;
            if (understandEv.HideLanguage)
                hideLanguage = true;

            var understanding = _prototype.Index(SharedLanguageSystem.MinimumFluency);
            var languageFont = HasComp<LanguageFontSuppressionComponent>(uid) && understanding.Understanding > 0;

            var (unwrappedMsg, wrappedMsg) = _chat.BuildComplexMessage(
                args.Message,
                RadioWrapper,
                args.Language,
                args.Speech.Bold,
                hideLanguage,
                languageFont,
                false,
                args.Name,
                args.Verb,
                args.Channel.LocalizedName,
                args.Channel.Color);

            var chat = new ChatMessage(
                ChatChannel.Radio,
                unwrappedMsg,
                wrappedMsg,
                NetEntity.Invalid,
                null);
            var chatMsg = new MsgChatMessage { Message = chat };

            _netMan.ServerSendMessage(chatMsg, actor.PlayerSession.Channel);
        }
    }

    private void OnIntrinsicSpeakLanguage(EntityUid uid,
        IntrinsicRadioTransmitterComponent component,
        EntitySpokeLanguageEvent args)
    {
        if (args.Channel != null && component.Channels.Contains(args.Channel.ID))
        {
            SendLanguageRadioMessage(uid, args.Message, args.Language, args.Channel, uid);
            args.Channel = null;
        }
    }

    public void SendLanguageRadioMessage(EntityUid uid,
        string message,
        ProtoId<RadioChannelPrototype> channel,
        EntityUid radioSource,
        bool escapeMarkup = true)
    {
        SendLanguageRadioMessage(uid, message, _prototype.Index(channel), radioSource, escapeMarkup);
    }

    public void SendLanguageRadioMessage(EntityUid uid,
        string message,
        RadioChannelPrototype channel,
        EntityUid radioSource,
        bool escapeMarkup = true)
    {
        var complex = new ComplexChatMessage(message, "\"", false, true, escapeMarkup);
        var language = _prototype.Index(_language.GetDefaultLanguage());
        SendLanguageRadioMessage(uid, complex, language, channel, radioSource);
    }

    public void SendLanguageRadioMessage(EntityUid messageSource,
        ComplexChatMessage message,
        LanguagePrototype language,
        RadioChannelPrototype channel,
        EntityUid radioSource)
    {
        if (!_messages.Add(message.OriginalMessage))
            return;

        var evt = new TransformSpeakerNameEvent(messageSource, MetaData(messageSource).EntityName);
        RaiseLocalEvent(messageSource, evt);

        var name = evt.VoiceName;
        name = FormattedMessage.EscapeText(name);

        SpeechVerbPrototype speech;
        if (evt.SpeechVerb != null && _prototype.Resolve(evt.SpeechVerb, out var evntProto))
            speech = evntProto;
        else
            speech = _chat.GetComplexSpeechVerb(messageSource, message);

        var verb = Loc.GetString(_random.Pick(speech.SpeechVerbStrings));

        var ev = new RadioReceiveLanguageEvent(message, language, speech, name, verb, messageSource, channel, radioSource);

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

            var needServer = !channel.LongRange && !sourceServerExempt;
            if (needServer && !hasActiveServer)
                continue;

            var attemptEv = new RadioReceiveAttemptEvent(channel, radioSource, receiver);
            RaiseLocalEvent(ref attemptEv);
            RaiseLocalEvent(receiver, ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            RaiseLocalEvent(receiver, ref ev);
        }

        var (unwrappedMessage, wrappedMessage) = _chat.BuildComplexMessage(message,
            RadioWrapper,
            language,
            speech.Bold,
            language.DisplayInChat,
            true,
            false,
            name,
            verb,
            channel.LocalizedName,
            channel.Color);

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} as {name} {channel.LocalizedName}: {unwrappedMessage}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} on {channel.LocalizedName}: {unwrappedMessage}");

        _replay.RecordServerMessage(new ChatMessage(
            ChatChannel.Radio,
            unwrappedMessage,
            wrappedMessage,
            NetEntity.Invalid,
            null));
        _messages.Remove(message.OriginalMessage);
    }
}
