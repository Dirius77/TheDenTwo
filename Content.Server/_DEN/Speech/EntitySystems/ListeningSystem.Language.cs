using Content.Server.Chat.Systems;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
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
        PingLanguageListeners(ev.Source, ev.LanguageEnt, ev.Message, ev.Verb, ev.ChatChannel);
    }

    public void PingLanguageListeners(EntityUid source, Entity<LanguageComponent> languageEnt, ComplexChatMessage message, string verb, ChatChannel channel)
    {
        // TODO whispering / audio volume? Microphone sensitivity?
        // for now, whispering just arbitrarily reduces the listener's max range.

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourceXform = xformQuery.GetComponent(source);
        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);

        var attemptEv = new ListenLanguageAttemptEvent(source, languageEnt);
        var ev = new ListenLanguageEvent(message, source, languageEnt, verb, channel);
        // TODO: Hardcoded obfuscation bad.
        var obfuscatedEv = channel == ChatChannel.Whisper
            ? new ListenLanguageEvent(_chat.ObfuscateComplexChatMessage(message, 0.2f), source, languageEnt, verb, channel)
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
