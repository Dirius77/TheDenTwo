using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Physics;

namespace Content.Shared._DEN.Language.EntitySystems;

public sealed partial class LineOfSightLanguageSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    private readonly CollisionGroup _sightMask = CollisionGroup.Opaque;

    private EntityQuery<GhostHearingComponent> _ghostHearings;

    public override void Initialize()
    {
        _ghostHearings = GetEntityQuery<GhostHearingComponent>();

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
