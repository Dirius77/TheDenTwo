using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Ghost;

namespace Content.Shared._DEN.Language.EntitySystems;

public sealed partial class WhisperMuffleSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;

    private EntityQuery<TransformComponent> _xforms;
    private EntityQuery<GhostHearingComponent> _ghostHearings;

    public override void Initialize()
    {
        _xforms = GetEntityQuery<TransformComponent>();
        _ghostHearings = GetEntityQuery<GhostHearingComponent>();

        SubscribeLocalEvent<WhisperMuffleComponent, LanguageModifyMessageEvent>(
            OnModifyMessage);
    }

    private void OnModifyMessage(Entity<WhisperMuffleComponent> ent, ref LanguageModifyMessageEvent args)
    {
        if (args.Channel != ChatChannel.Whisper || _ghostHearings.HasComp(args.Listener))
            return;

        var sourceCoords = _xforms.GetComponent(args.Sender).Coordinates;
        var listenXform = _xforms.GetComponent(args.Listener);
        if (!sourceCoords.TryDistance(EntityManager, listenXform.Coordinates, out var distance))
            return;

        if (distance <= SharedChatSystem.WhisperClearRange)
            return;

        if (_examine.InRangeUnOccluded(args.Sender, args.Listener, SharedChatSystem.WhisperMuffledRange))
        {
            if (ent.Comp.Muffle)
            {
                args.Message = _chat.ObfuscateComplexChatMessage(args.Message, ent.Comp.MuffleAmount);
            }
            else
            {
                args.Message = new ComplexChatMessage(args.Message, []);
            }
        }
        else
        {
            args.Name = "Someone";
            if (ent.Comp.Muffle)
            {
                args.Message = _chat.ObfuscateComplexChatMessage(args.Message, ent.Comp.MuffleAmount);
            }
            else
            {
                args.Message = new ComplexChatMessage(args.Message, []);
            }
        }
    }
}
