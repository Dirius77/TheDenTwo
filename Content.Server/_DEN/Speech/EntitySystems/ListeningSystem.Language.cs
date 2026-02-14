using Content.Server.Chat.Systems;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Speech;
using Content.Shared.Chat;
using Content.Shared.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class ListeningSystem
{
    [Dependency] private readonly SharedChatSystem _chat = default!;

    private void InitializeLanguage()
    {
        SubscribeLocalEvent<EntitySpokeLanguageEvent>(OnSpeakLanguage);
    }

    private void OnSpeakLanguage(EntitySpokeLanguageEvent ev)
    {
        PingLanguageListeners(ev.Source, ev.Message, ev.Language, ev.Whisper);
    }

    public void PingLanguageListeners(EntityUid source, SharedChatSystem.ComplexChatMessage message, LanguagePrototype language, bool whisper)
    {
        // TODO whispering / audio volume? Microphone sensitivity?
        // for now, whispering just arbitrarily reduces the listener's max range.

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourceXform = xformQuery.GetComponent(source);
        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);

        var attemptEv = new ListenLanguageAttemptEvent(source, language);
        var ev = new ListenLanguageEvent(message, source, language);
        var obfuscatedEv = whisper
            ? new ListenLanguageEvent(_chat.ObfuscateComplexChatMessage(message, 0.2f), source, language)
            : null;
        var query = EntityQueryEnumerator<ActiveListenerComponent, TransformComponent>();

        while (query.MoveNext(out var listenerUid, out var listener, out var xform))
        {
            if (xform.MapID != sourceXform.MapID)
                continue;

            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).LengthSquared();
            if (distance > listener.Range * listener.Range)
                continue;

            RaiseLocalEvent(listenerUid, attemptEv);
            if (attemptEv.Cancelled)
            {
                attemptEv.Uncancel();
                continue;
            }

            if (obfuscatedEv != null && distance > ChatSystem.WhisperClearRange)
                RaiseLocalEvent(listenerUid, obfuscatedEv);
            else
                RaiseLocalEvent(listenerUid, ev);
        }

    }
}
