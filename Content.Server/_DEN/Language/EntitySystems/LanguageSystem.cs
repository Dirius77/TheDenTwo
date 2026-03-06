using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared.Chat;
using Robust.Shared.Prototypes;

namespace Content.Server._DEN.Language.EntitySystems;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageComponent, LanguageRelayedEvent<AttemptUnderstandingEvent>>(
            OnAttemptUnderstandingRelay);

        SubscribeNetworkEvent<HideFontsMessage>(OnHideFontsRequest);
    }

    private void OnHideFontsRequest(HideFontsMessage msg, EntitySessionEventArgs args)
    {
        var senderSession = args.SenderSession;

        if (senderSession.AttachedEntity is not { } senderEnt)
            return;

        if (msg.Hide)
        {
            EnsureComp<LanguageFontSuppressionComponent>(senderEnt);
        }
        else
        {
            RemComp<LanguageFontSuppressionComponent>(senderEnt);
        }
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
        string originalVerb,
        ChatChannel chatChannel,
        out string name,
        out string verb)
    {
        var ev = new LanguageModifyMessageEvent(sender, listener, originalMessage, language, understanding, originalName, originalVerb, chatChannel);
        RaiseLocalEvent(languageEntity, ev);
        name = ev.Name;
        verb = ev.Verb;
        return ev.Message;
    }
}
