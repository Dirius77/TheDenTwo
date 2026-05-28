using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Physics;

namespace Content.Shared._DEN.Language.EntitySystems;

public sealed partial class LineOfSightLanguageSystem : EntitySystem
{
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private EntityQuery<GhostHearingComponent> _ghostHearings = default!;

    private readonly CollisionGroup _sightMask = CollisionGroup.Opaque;

    public override void Initialize()
    {
        SubscribeLocalEvent<LineOfSightLanguageComponent, LanguageModifyMessageEvent>(
            OnModifyMessage);
    }

    private void OnModifyMessage(Entity<LineOfSightLanguageComponent> entity,
        ref LanguageModifyMessageEvent evt)
    {
        var isWhisper = evt.Channel == ChatChannel.Whisper;
        if (!(_ghostHearings.HasComp(evt.Listener) && !isWhisper) && !_interactionSystem.InRangeUnobstructed(evt.Sender,
                evt.Listener,
                isWhisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange,
                _sightMask))
        {
            evt.Message = new ComplexChatMessage(evt.Message, []);
        }
    }
}
