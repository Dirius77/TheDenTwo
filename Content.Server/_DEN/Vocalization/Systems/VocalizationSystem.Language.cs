using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Server.Vocalization.Components;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared.Chat;

namespace Content.Server.Vocalization.Systems;

public sealed partial class VocalizationSystem
{
    [Dependency] private readonly SharedLanguageSystem _languageSystem = default!;

    private void InitializeLanguage()
    {
        SubscribeLocalEvent<VocalizerRequiresPowerComponent, TryVocalizeLanguageEvent>(
            OnRequiresPowerTryVocalizeLanguage);
    }

    private void OnRequiresPowerTryVocalizeLanguage(Entity<VocalizerRequiresPowerComponent> ent,
        ref TryVocalizeLanguageEvent args)
    {
        if (!TryComp<ApcPowerReceiverComponent>(ent, out var receiver))
            return;

        args.Cancelled |= !receiver.Powered;
    }

    private void TrySpeakLanguage(Entity<VocalizerComponent> entity)
    {
        var tryVocalizeLanguageEvent = new TryVocalizeLanguageEvent();
        RaiseLocalEvent(entity, ref tryVocalizeLanguageEvent);

        if (tryVocalizeLanguageEvent.Cancelled)
            return;

        if (!tryVocalizeLanguageEvent.Handled)
            return;

        if (tryVocalizeLanguageEvent.Message is not { } message)
            return;

        var language = tryVocalizeLanguageEvent.Language ?? _languageSystem.GetCurrentLanguageEntity(entity);
        if (language is null)
            return;

        SpeakLanguage(entity, language.Value, message);
    }

    private void SpeakLanguage(Entity<VocalizerComponent> entity, Entity<LanguageComponent> language, string message)
    {
        var vocalizeLanguageEvent = new VocalizeLanguageEvent(message, language);
        RaiseLocalEvent(entity, ref vocalizeLanguageEvent);

        if (vocalizeLanguageEvent.Handled)
            return;

        // I skip the CanSpeak check because TrySendInGameICMessage does chat check anyway and it's not necessarily
        // trivial with languages, doing it twice is pointless.
        var cmplxMessage = _chat.ConvertMessageToComplex(message);
        _chat.SendEntityComplexSpeech(entity, cmplxMessage, ChatSystem.SpeakWrapper, ChatChannel.Local, entity.Comp.HideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal, languageOverride: language);
    }
}

/// <summary>
/// Fired when the entity wants to try vocalizing, but doesn't have a message yet.
/// </summary>
/// <param name="Message">Message to send, this is null when the event is just fired and should be set by a system</param>
/// <param name="Language">Language to use, this is null when the event is just fired and may be set by a system</param>
/// <param name="Handled">Whether the message was handled by a system</param>
/// <param name="Cancelled">Prevents the vocalization attempt</param>
[ByRefEvent]
public record struct TryVocalizeLanguageEvent(string? Message = null, Entity<LanguageComponent>? Language = null, bool Handled = false, bool Cancelled = false);

/// <summary>
/// Fired when the entity wants to vocalize and has a message. Allows for interception by other systems if the
/// vocalization needs to be done some other way
/// </summary>
/// <param name="Message">Message to send</param>
/// <param name="Language">Language to use</param>
/// <param name="Handled">Whether the message was handled by a system</param>
[ByRefEvent]
public record struct VocalizeLanguageEvent(string Message, Entity<LanguageComponent> Language, bool Handled = false);
