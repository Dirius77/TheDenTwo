using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Speech.Components;

namespace Content.Shared.Speech.EntitySystems;

/// <summary>
/// This system redirects local chat messages to listening entities (e.g., radio microphones).
/// </summary>
public sealed partial class ListeningSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _xforms = default!;
    [Dependency] private SharedChatSystem _chat = default!;

    [SubscribeLocalEvent]
    private void OnSpeak(EntitySpokeEvent ev)
    {
        PingListeners(ev.Source, ev.LanguageEnt, ev.Message, ev.Verb, ev.ChatChannel); // DEN: Languages
    }

    /// <summary>
    /// Sends a speech message to entities listening within range.
    /// </summary>
    public void PingListeners(EntityUid source, Entity<LanguageComponent> languageEnt, ComplexChatMessage message, string verb, ChatChannel channel) // DEN: Languages
    {
        // TODO whispering / audio volume? Microphone sensitivity?
        // for now, whispering just arbitrarily reduces the listener's max range.

        var sourceXform = Transform(source);
        var sourcePos = _xforms.GetWorldPosition(sourceXform);

        var attemptEv = new ListenAttemptEvent(source, languageEnt); // DEN: Languages
        var ev = new ListenEvent(message, source, languageEnt, verb, channel); // DEN: Languages
        // TODO: Hardcoded obfuscation bad.
        var obfuscatedEv = channel == ChatChannel.Whisper
            ? new ListenEvent(_chat.ObfuscateComplexChatMessage(message, 0.2f), source, languageEnt, verb, channel)
            : null; // DEN: Languages. Use complex obfuscation and language.
        var query = EntityQueryEnumerator<ActiveListenerComponent, TransformComponent>();

        while(query.MoveNext(out var listenerUid, out var listener, out var xform))
        {
            if (xform.MapID != sourceXform.MapID)
                continue;

            // range checks
            // TODO proper speech occlusion
            var distance = (sourcePos - _xforms.GetWorldPosition(xform)).LengthSquared();
            if (distance > listener.Range * listener.Range)
                continue;

            RaiseLocalEvent(listenerUid, attemptEv);
            if (attemptEv.Cancelled)
            {
                attemptEv.Uncancel();
                continue;
            }

            if (obfuscatedEv != null && distance > SharedChatSystem.WhisperClearRange)
                RaiseLocalEvent(listenerUid, obfuscatedEv);
            else
                RaiseLocalEvent(listenerUid, ev);
        }
    }
}
