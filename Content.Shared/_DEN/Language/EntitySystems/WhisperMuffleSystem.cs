using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Ghost.Components;

namespace Content.Shared._DEN.Language.EntitySystems;

public sealed partial class WhisperMuffleSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private EntityQuery<GhostHearingComponent> _ghostHearings = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WhisperMuffleComponent, LanguageModifyMessageEvent>(
            OnModifyMessage);
    }

    private void OnModifyMessage(Entity<WhisperMuffleComponent> ent, ref LanguageModifyMessageEvent args)
    {
        if (args.Channel != ChatChannel.Whisper || _ghostHearings.HasComp(args.Listener))
            return;

        var sourceCoords = Transform(args.Sender).Coordinates;
        var listenXform = Transform(args.Listener);
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
