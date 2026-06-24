using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._DEN.Language.EntitySystems;

public sealed partial class MinimumFluencySystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MinimumFluencyComponent, LanguageModifyMessageEvent>(OnLanguageModifyMessage);
    }

    private void OnLanguageModifyMessage(Entity<MinimumFluencyComponent> ent, ref LanguageModifyMessageEvent args)
    {
        var minFluency = _proto.Index(ent.Comp.MinimumFluency);
        if (args.Understanding >= minFluency)
            return;

        if (ent.Comp.Replacements is { } replacements)
        {
            var (kind, list) = _random.Pick(replacements);
            var chosen = _random.Pick(list);
            args.Message = new ComplexChatMessage(args.Message, [(kind, Loc.GetString(chosen))]);
        }
        else
        {
            args.Message = new ComplexChatMessage(args.Message, []);
        }
    }
}
