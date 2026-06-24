using Content.Shared._DEN.Language.Components;

namespace Content.Shared._DEN.Language.EntitySystems;

public sealed partial class ReplaceSpeakerNameSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ReplaceSpeakerNameComponent, LanguageModifyMessageEvent>(OnLanguageModifyMessage);
    }

    private void OnLanguageModifyMessage(Entity<ReplaceSpeakerNameComponent> ent, ref LanguageModifyMessageEvent args)
    {
        args.Name = ent.Comp.ReplaceName ?? "";
    }
}
