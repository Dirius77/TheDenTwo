using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Robust.Shared.Utility;

namespace Content.Shared._DEN.Language.EntitySystems;

public sealed partial class VisualNameSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<VisualNameComponent, LanguageRelayedEvent<TransformSpeakerNameEvent>>(
            OnTransformSpeakerName);
    }

    private void OnTransformSpeakerName(Entity<VisualNameComponent> ent,
        ref LanguageRelayedEvent<TransformSpeakerNameEvent> args)
    {
        var evt = args.Args;
        var ident = Identity.Entity(evt.Sender, EntityManager);
        evt.VoiceName = FormattedMessage.EscapeText(Name(ident));
    }
}
