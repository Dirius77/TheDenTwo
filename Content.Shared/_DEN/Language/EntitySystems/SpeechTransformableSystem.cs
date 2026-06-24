using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;

namespace Content.Shared._DEN.Language.EntitySystems;

public sealed partial class SpeechTransformableSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SpeechTransformableComponent, LanguageRelayedEvent<TransformLanguageEvent>>(OnVerbalLanguageTransform);
    }

    private void OnVerbalLanguageTransform(Entity<SpeechTransformableComponent> entity, ref LanguageRelayedEvent<TransformLanguageEvent> args)
    {
        var evt = args.Args;
        var processedMessages = new List<(ChatPart, string)>();
        foreach (var (kind, part) in evt.Message.Parts)
        {
            if (kind == ChatPart.Dialog)
            {
                var ev = new TransformSpeechEvent(evt.Sender, part);
                RaiseLocalEvent(evt.Sender, ev, true);
                if (string.IsNullOrEmpty(ev.Message))
                    continue;
                processedMessages.Add((kind, ev.Message));
            }
            else
            {
                processedMessages.Add((kind, part));
            }
        }

        evt.Message = new ComplexChatMessage(evt.Message, processedMessages);
    }
}
