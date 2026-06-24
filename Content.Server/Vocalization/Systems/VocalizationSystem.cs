using Content.Server._DEN.Language.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Server.Vocalization.Components;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Vocalization.Systems;

/// <summary>
/// VocalizationSystem raises VocalizeEvents to make entities speak at certain intervals
/// This is used in combination with systems like ParrotMemorySystem to randomly say messages from memory,
/// or can be used by other systems to speak pre-set messages
/// </summary>
public sealed partial class VocalizationSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private LanguageSystem _languageSystem = default!; // DEN: Languages
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VocalizerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VocalizerRequiresPowerComponent, TryVocalizeEvent>(OnRequiresPowerTryVocalize);
    }

    private void OnMapInit(Entity<VocalizerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextVocalizeInterval = _random.Next(ent.Comp.MinVocalizeInterval, ent.Comp.MaxVocalizeInterval);
    }

    private void OnRequiresPowerTryVocalize(Entity<VocalizerRequiresPowerComponent> ent, ref TryVocalizeEvent args)
    {
        if (!TryComp<ApcPowerReceiverComponent>(ent, out var receiver))
            return;

        args.Cancelled |= !receiver.Powered;
    }

    /// <summary>
    /// Try speaking by raising a TryVocalizeEvent
    /// This event is passed to systems adding a message to it and setting it to handled
    /// </summary>
    private void TrySpeak(Entity<VocalizerComponent> entity)
    {
        var tryVocalizeEvent = new TryVocalizeEvent();
        RaiseLocalEvent(entity.Owner, ref tryVocalizeEvent);

        // If the event was cancelled, don't speak
        if (tryVocalizeEvent.Cancelled)
            return;

        // if the event was never handled, return
        // this happens if there are no components that trigger systems to add a message to this event
        if (!tryVocalizeEvent.Handled)
            return;

        // if the event's message is null for whatever reason, return.
        // this would mean a system didn't set the message properly but did set the event to handled
        if (tryVocalizeEvent.Message is not { } message)
            return;

        // DEN: Languages
        var language = tryVocalizeEvent.Language ?? _languageSystem.GetCurrentLanguageEntity(entity);
        if (language is null)
            return;

        Speak(entity, language.Value, message); // DEN: Languages
    }

    /// <summary>
    /// Actually say something.
    /// </summary>
    private void Speak(Entity<VocalizerComponent> entity, Entity<LanguageComponent> language, string message) // DEN: Languages
    {
        // raise a VocalizeEvent
        // this can be handled by other systems to speak using a method other than local chat
        var vocalizeEvent = new VocalizeEvent(message, language); // DEN: Languages
        RaiseLocalEvent(entity.Owner, ref vocalizeEvent);

        // if the event is handled, don't try speaking
        if (vocalizeEvent.Handled)
            return;

        // DEN Start: Convert to complex and send.
        // There used to be a "Can Speak" check here, but sending the speech already does that check and with languages
        // it is not always trivial, so doing it twice is a waste.
        var cmplxMessage = _chat.ConvertMessageToComplex(message);
        _chat.SendEntityComplexSpeech(entity, cmplxMessage, ChatSystem.SpeakWrapper, ChatChannel.Local, entity.Comp.HideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal, languageOverride: language);
        // DEN End.
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // get current game time for delay
        var currentGameTime = _gameTiming.CurTime;

        // query to get all entities with a VocalizeComponent
        var query = EntityQueryEnumerator<VocalizerComponent>();
        while (query.MoveNext(out var uid, out var vocalizer))
        {
            // go to next entity if it is too early for this one to speak
            if (currentGameTime < vocalizer.NextVocalizeInterval)
                continue;

            // set a new time for the speak interval, regardless of whether speaking works
            var randomSpeakInterval = _random.Next(vocalizer.MinVocalizeInterval, vocalizer.MaxVocalizeInterval);
            vocalizer.NextVocalizeInterval += randomSpeakInterval;

            // if an admin updates the speak interval to be immediate, this loop will spam messages until the
            // nextspeakinterval catches up with the current game time. Prevent this from happening
            if (vocalizer.NextVocalizeInterval < _gameTiming.CurTime)
                vocalizer.NextVocalizeInterval = _gameTiming.CurTime + randomSpeakInterval;

            // try to speak
            TrySpeak((uid, vocalizer));
        }
    }
}

/// <summary>
/// Fired when the entity wants to try vocalizing, but doesn't have a message yet
/// </summary>
/// <param name="Message">Message to send, this is null when the event is just fired and should be set by a system</param>
/// <param name="Language">Language to use, this is null when the event is just fired and may be set by a system.</param>
/// <param name="Handled">Whether the message was handled by a system</param>
[ByRefEvent]
public record struct TryVocalizeEvent(string? Message = null, Entity<LanguageComponent>? Language = null, bool Handled = false, bool Cancelled = false); // DEN: Add Languages

/// <summary>
/// Fired when the entity wants to vocalize and has a message. Allows for interception by other systems if the
/// vocalization needs to be done some other way
/// </summary>
/// <param name="Message">Message to send</param>
/// <param name="Language">Language entity to use</param>
/// <param name="Handled">Whether the message was handled by a system</param>
[ByRefEvent]
public record struct VocalizeEvent(string Message, Entity<LanguageComponent> Language, bool Handled = false); // DEN: Languages
