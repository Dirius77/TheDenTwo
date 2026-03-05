using Content.Server.Chat.Systems;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared._DEN.Speech;
using Content.Shared.Cargo;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Speech;
using Content.Shared.Telephone;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Telephone;

public sealed partial class TelephoneSystem
{
    public static readonly ProtoId<LanguageWrapperPrototype> TelephoneWrapper = "TelephoneWrapper";

    private EntityQuery<AudibleComponent> _audibleQuery;
    private EntityQuery<LineOfSightLanguageComponent> _losQuery;

    private void InitializeLanguage()
    {
        _audibleQuery = GetEntityQuery<AudibleComponent>();
        _losQuery = GetEntityQuery<LineOfSightLanguageComponent>();

        SubscribeLocalEvent<TelephoneComponent, ListenLanguageAttemptEvent>(OnAttemptLanguageListen);
        SubscribeLocalEvent<TelephoneComponent, ListenLanguageEvent>(OnLanguageListen);
        SubscribeLocalEvent<TelephoneComponent, TelephoneMessageLanguageReceivedEvent>(OnTelephoneMessageLanguageReceived);
    }

    private void OnAttemptLanguageListen(Entity<TelephoneComponent> entity, ref ListenLanguageAttemptEvent args)
    {
        if (!IsTelephonePowered(entity) ||
            !IsTelephoneEngaged(entity) ||
            entity.Comp.Muted ||
            !_interaction.InRangeUnobstructed(args.Source, entity.Owner, 0))
        {
            args.Cancel();
        }
    }

    private void OnLanguageListen(Entity<TelephoneComponent> entity, ref ListenLanguageEvent args)
    {
        if (args.Source == entity.Owner)
            return;

        // Everything else in the chat code checks for ActorComponent...
        if (!HasComp<MindContainerComponent>(args.Source))
            return;

        if (!_recentChatMessages.Add((args.Source, args.Message.OriginalMessage, entity)))
            return;

        // Only transmit spoken languages, or, in the case of holopads, sign and spoken.
        if (_audibleQuery.HasComp(args.LanguageEnt) || entity.Comp.TransmitsVisuals && _losQuery.HasComp(args.LanguageEnt))
            SendTelephoneLanguageMessage(args.Source, args.LanguageEnt, args.Message, entity);
    }

    private void OnTelephoneMessageLanguageReceived(Entity<TelephoneComponent> entity,
        ref TelephoneMessageLanguageReceivedEvent args)
    {
        // Prevent message feedback loops
        if (entity == args.TelephoneSource)
            return;

        if (!IsTelephonePowered(entity) ||
            !IsSourceConnectedToReceiver(args.TelephoneSource, entity))
            return;

        var nameEv = new TransformSpeakerNameEvent(args.MessageSource, Name(args.MessageSource));
        RaiseLocalEvent(args.MessageSource, nameEv);

        // Determine if speech should be relayed via the telephone itself or a designated speaker
        var speaker = entity.Comp.Speaker?.Owner ?? entity.Owner;

        var name = Loc.GetString("chat-telephone-name-relay",
            ("originalName", nameEv.VoiceName),
            ("speaker", Name(speaker)));

        var range = args.TelephoneSource.Comp.LinkedTelephones.Count > 1
            ? ChatTransmitRange.HideChat
            : ChatTransmitRange.GhostRangeLimit;
        var whisper = entity.Comp.SpeakerVolume == TelephoneVolume.Whisper;

        _chat.SendEntityComplexSpeech(speaker, args.Message, TelephoneWrapper, whisper ? ChatChannel.Whisper : ChatChannel.Local, range, null, name, languageOverride: args.LanguageEnt);
    }

    private void SendTelephoneLanguageMessage(EntityUid messageSource, Entity<LanguageComponent> languageEnt, ComplexChatMessage message, Entity<TelephoneComponent> source)
    {
        // This method assumes that you've already checked that this
        // telephone is able to transmit messages and that it can
        // send messages to any telephones linked to it
        var language = _prototype.Index(languageEnt.Comp.Language);

        var ev = new TransformSpeakerNameEvent(messageSource, MetaData(messageSource).EntityName);
        RaiseLocalEvent(messageSource, ev);

        var name = ev.VoiceName;
        name = FormattedMessage.EscapeText(name);

        SpeechVerbPrototype speech;
        if (ev.SpeechVerb != null && _prototype.Resolve(ev.SpeechVerb, out var evntProto))
            speech = evntProto;
        else
            speech = _chat.GetComplexSpeechVerb(messageSource, message, language, ChatChannel.Radio);

        var verb = Loc.GetString(_random.Pick(speech.SpeechVerbStrings));

        var evSentMessage = new TelephoneMessageLanguageSentEvent(message, languageEnt, messageSource);
        RaiseLocalEvent(source, ref evSentMessage);
        source.Comp.StateStartTime = _timing.CurTime;

        var evReceivedMessage = new TelephoneMessageLanguageReceivedEvent(message, languageEnt, verb, name, messageSource, source);

        foreach (var receiver in source.Comp.LinkedTelephones)
        {
            RaiseLocalEvent(receiver, ref evReceivedMessage);
            receiver.Comp.StateStartTime = _timing.CurTime;
        }

        var (unwrappedMessage, wrappedMessage) = _chat.BuildComplexMessage(message,
            _prototype.Index(TelephoneWrapper),
            language,
            speech.Bold,
            language.DisplayInChat,
            true,
            name,
            verb,
            null,
            null);

        var chat = new ChatMessage(
            ChatChannel.Radio,
            unwrappedMessage,
            wrappedMessage,
            NetEntity.Invalid,
            null);

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Telephone message from {ToPrettyString(messageSource):user} as {name} on {source} in {language.LocalizedName}: {unwrappedMessage}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Telephone message from {ToPrettyString(messageSource):user} on {source} in {language.LocalizedName}: {unwrappedMessage}");

        _replay.RecordServerMessage(chat);
    }
}
