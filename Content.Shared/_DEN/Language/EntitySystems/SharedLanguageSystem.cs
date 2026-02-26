using Content.Shared._DEN.CCVars;
using Content.Shared._DEN.Language.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language.EntitySystems;

public abstract partial class SharedLanguageSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public static readonly ProtoId<LanguageFluencyPrototype> MaximumFluency = "Fluent";
    public static readonly ProtoId<LanguageFluencyPrototype> MinimumFluency = "Unfamiliar";

    private static ProtoId<LanguagePrototype> _defaultLanguage = "Basic";
    private bool _fallbackDefaultLanguage;

    private EntityQuery<LanguageComponent> _languageQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageCommunicatorComponent, ComponentInit>(OnLanguageCommunicatorInit);
        SubscribeLocalEvent<LanguageCommunicatorComponent, ComponentShutdown>(OnLanguageCommunicatorShutdown);
        SubscribeLocalEvent<LanguageCommunicatorComponent, EntInsertedIntoContainerMessage>(
            OnLanguageCommunicatorEntityInserted);

        SubscribeLocalEvent<LanguageComponent, ComponentShutdown>(OnLanguageShutdown);

        _cfg.OnValueChanged(DenCCVars.FallbackDefaultLanguage, fallback => _fallbackDefaultLanguage = fallback, true);
        _cfg.OnValueChanged(DenCCVars.DefaultLanguage, lang => _defaultLanguage = lang, true);

        _languageQuery = GetEntityQuery<LanguageComponent>();
    }

    private void OnLanguageCommunicatorShutdown(Entity<LanguageCommunicatorComponent> ent, ref ComponentShutdown evt)
    {
        if (ent.Comp.Languages is { } container)
            _container.ShutdownContainer(container);
    }

    private void OnLanguageCommunicatorInit(Entity<LanguageCommunicatorComponent> ent, ref ComponentInit evt)
    {
        ent.Comp.Languages = _container.EnsureContainer<Container>(ent, LanguageCommunicatorComponent.ContainerId);

        foreach (var (language, (speaks, fluency)) in ent.Comp.BaseLanguages)
        {
            TryAddLanguage(ent, language, speaks, fluency, out _);
        }
    }

    private void OnLanguageCommunicatorEntityInserted(Entity<LanguageCommunicatorComponent> ent,
        ref EntInsertedIntoContainerMessage args)
    {

    }

    private void OnLanguageShutdown(Entity<LanguageComponent> ent, ref ComponentShutdown evt)
    {
        if (TryComp<LanguageCommunicatorComponent>(ent.Comp.Holder, out var commComp) &&
            commComp.CurrentLanguage == ent)
            commComp.CurrentLanguage = null;

        foreach (var child in ent.Comp.Children)
        {
            QueueDel(child);
        }
    }

    private bool InsertLanguageAndChildren(EntityUid target,
        ProtoId<LanguagePrototype> languageProto,
        ProtoId<LanguageFluencyPrototype> fluencyProto,
        bool speaks,
        out List<EntityUid> addedEntities)
    {
        addedEntities = new();

        if (!_proto.TryIndex(languageProto, out var language) || !_proto.TryIndex(fluencyProto, out var fluency))
            return false;

        var communicator = EnsureComp<LanguageCommunicatorComponent>(target);
        if (communicator.Languages is not { } languages)
            return false;

        var entity = SpawnLanguageEntity(languageProto, fluencyProto, speaks);
        entity.Comp.Holder = target;
        if (!_container.Insert(entity.AsType(), languages))
            return false;

        if (fluency < _proto.Index(MaximumFluency))
        {
            addedEntities.Add(entity);
            return true;
        }

        var failedChild = false;
        foreach (var (relatedLang, relatedFluency) in language.RelatedLanguages)
        {
            var childEnt = SpawnLanguageEntity(relatedLang, relatedFluency, false);
            childEnt.Comp.Holder = target;

            if (!_container.Insert(childEnt.AsType(), languages))
            {
                failedChild = true;
                continue;
            }

            entity.Comp.Children.Add(childEnt);
            addedEntities.Add(childEnt);
        }

        return failedChild;
    }

    private Entity<LanguageComponent> SpawnLanguageEntity(ProtoId<LanguagePrototype> languageProto,
        ProtoId<LanguageFluencyPrototype> fluencyProto,
        bool speaks)
    {
        var language = _proto.Index(languageProto);

        var languageEnt = Spawn();
        var languageComp = EnsureComp<LanguageComponent>(languageEnt);
        languageComp.Fluency = fluencyProto;
        languageComp.Language = languageProto;
        languageComp.Speaks = speaks;
        if (language.LanguageComponents is not null)
            EntityManager.AddComponents(languageEnt, language.LanguageComponents);

        return (languageEnt, languageComp);
    }
}
