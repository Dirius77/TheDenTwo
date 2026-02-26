using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Robust.Shared.Prototypes;

namespace Content.Server._DEN.Language.EntitySystems;

public sealed partial class LanguageSystem : Shared._DEN.Language.EntitySystems.SharedLanguageSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageComponent, LanguageRelayedEvent<AttemptUnderstandingEvent>>(
            OnAttemptUnderstandingRelay);
    }

    private void OnAttemptUnderstandingRelay(Entity<LanguageComponent> ent,
        ref LanguageRelayedEvent<AttemptUnderstandingEvent> args)
    {
        var evt = args.Args;
        if (evt.Language.ID != ent.Comp.Language)
            return;

        var hasUnderstanding = _proto.Index(ent.Comp.Fluency);
        if (evt.Understanding is null || _proto.Index(evt.Understanding.Value.Comp.Fluency) < hasUnderstanding)
        {
            evt.Understanding = ent;
            evt.Handled = true;
        }
    }

    public ComplexChatMessage ModifyMessageWithLanguage(EntityUid languageEntity,
        EntityUid sender,
        EntityUid listener,
        ComplexChatMessage originalMessage,
        LanguagePrototype language,
        LanguageFluencyPrototype understanding,
        string originalName,
        bool isWhisper,
        out string name)
    {
        var ev = new LanguageModifyMessageEvent(sender, listener, originalMessage, language, understanding, originalName, isWhisper);
        RaiseLocalEvent(languageEntity, ev);
        name = ev.Name;
        return ev.Message;
    }
}
