using Content.Shared._DEN.Language.Components;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language.EntitySystems;

public sealed partial class ChildLanguageSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedLanguageSystem _language = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChildLanguageComponent, ExaminedEvent>(OnChildLanguageExamined);

        SubscribeLocalEvent<ChildLanguageComponent, ComponentStartup>(OnChildLanguageStartup);
    }

    private void OnChildLanguageStartup(Entity<ChildLanguageComponent> childLang, ref ComponentStartup args)
    {
        _language.OnLanguageUpdated(childLang.AsType());
    }

    private void OnChildLanguageExamined(Entity<ChildLanguageComponent> lang, ref ExaminedEvent args)
    {
        if (!TryComp<LanguageComponent>(lang.Comp.ParentLanguage, out var parentLanguage))
            return;

        var parentLang = _proto.Index(parentLanguage.Language);
        args.PushMarkup(Loc.GetString("language-child-language-examine", ("parent", parentLang.LocalizedName)));
    }
}
