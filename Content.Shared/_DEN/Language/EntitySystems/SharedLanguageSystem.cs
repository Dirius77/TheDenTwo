using Content.Shared._DEN.CCVar;
using Content.Shared._DEN.Language.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language.EntitySystems;

public abstract partial class SharedLanguageSystem : EntitySystem
{
    public bool LanguagesEnabled { get; private set; }

    [Dependency] protected IConfigurationManager _cfg = default!;
    [Dependency] protected SharedContainerSystem _container = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private INetManager _netMan = default!;
    [Dependency] private EntityQuery<LanguageComponent> _languageQuery = default!;

    public static readonly ProtoId<LanguageFluencyPrototype> MaximumFluency = "Fluent";
    public static readonly ProtoId<LanguageFluencyPrototype> MinimumFluency = "Unfamiliar";
    
    public static readonly ProtoId<LanguagePrototype> DisabledLanguage = "Default";
    public static ProtoId<LanguagePrototype> DefaultLanguage = "Basic";
    public static ProtoId<LanguageEntryPrototype> DefaultLanguageEntry = "Basic";
    public static int LanguageSelectionPoints = 6;
    
    private bool _fallbackDefaultLanguage;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageCommunicatorComponent, ComponentInit>(OnLanguageCommunicatorCompInit);
        SubscribeLocalEvent<LanguageCommunicatorComponent, MapInitEvent>(OnLanguageCommunicatorMapInit);
        SubscribeLocalEvent<LanguageCommunicatorComponent, ComponentShutdown>(OnLanguageCommunicatorShutdown);
        SubscribeLocalEvent<LanguageCommunicatorComponent, EntInsertedIntoContainerMessage>(
            OnLanguageCommunicatorEntityInserted);
        SubscribeLocalEvent<LanguageCommunicatorComponent, EntRemovedFromContainerMessage>(
            OnLanguageCommunicatorEntityRemoved);

        SubscribeLocalEvent<LanguageComponent, ComponentShutdown>(OnLanguageShutdown);

        SubscribeAllEvent<RequestSetSpokenLanguageEvent>(OnRequestSetSpokenLanguage);

        _cfg.OnValueChanged(DenCCVars.FallbackDefaultLanguage, fallback => _fallbackDefaultLanguage = fallback, true);
        _cfg.OnValueChanged(DenCCVars.DefaultLanguage, lang => DefaultLanguage = lang, true);
        _cfg.OnValueChanged(DenCCVars.DefaultLanguageEntry, langEnt => DefaultLanguageEntry = langEnt, true);
        _cfg.OnValueChanged(DenCCVars.LanguageSelectionPoints, points => LanguageSelectionPoints = points, true);
        _cfg.OnValueChanged(DenCCVars.LanguageEnabled, OnLanguageEnableChanged, true);
    }

    private void OnLanguageEnableChanged(bool enabled)
    {
        LanguagesEnabled = enabled;

        if (!enabled)
            return;

        var query = EntityQueryEnumerator<LanguageCommunicatorComponent>();
        while(query.MoveNext(out var entity, out _))
        {
            if (!TryGetLanguageEntities(entity, DisabledLanguage, out var languages))
                continue;

            foreach (var lang in languages)
            {
                PredictedQueueDel(lang);
            }
        }
    }

    private void OnRequestSetSpokenLanguage(RequestSetSpokenLanguageEvent evt, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var languageEnt = GetEntity(evt.LanguageEntity);

        if (!TryComp<LanguageComponent>(languageEnt, out var langComp) || langComp.Holder != user)
            return;

        TrySetLanguage(user, (languageEnt, langComp));
    }

    private void OnLanguageCommunicatorCompInit(Entity<LanguageCommunicatorComponent> ent, ref ComponentInit evt)
    {
        ent.Comp.Languages = _container.EnsureContainer<Container>(ent, LanguageCommunicatorComponent.ContainerId);
    }

    private void OnLanguageCommunicatorMapInit(Entity<LanguageCommunicatorComponent> ent, ref MapInitEvent evt)
    {
        if (!LanguagesEnabled)
            return;

        foreach (var (language, (speaks, fluency)) in ent.Comp.BaseLanguages)
        {
            TryAddLanguage(ent, language, fluency, speaks, out _);
        }
    }

    private void OnLanguageCommunicatorShutdown(Entity<LanguageCommunicatorComponent> ent, ref ComponentShutdown evt)
    {
        if (ent.Comp.Languages is { } container)
            _container.ShutdownContainer(container);
    }

    private void OnLanguageCommunicatorEntityInserted(Entity<LanguageCommunicatorComponent> ent,
        ref EntInsertedIntoContainerMessage args)
    {
        if (_languageQuery.TryComp(args.Entity, out var langComp))
        {
            var addEvt = new LanguageAddedToCommunicatorEvent((args.Entity, langComp));
            RaiseLocalEvent(ent.Owner, addEvt);
        }
    }

    private void OnLanguageCommunicatorEntityRemoved(Entity<LanguageCommunicatorComponent> ent,
        ref EntRemovedFromContainerMessage args)
    {
        if (_languageQuery.TryComp(args.Entity, out var langComp))
        {
            OnLanguageRemoved(ent, (args.Entity, langComp));

            var remEvt = new LanguageRemovedFromCommunicatorEvent((args.Entity, langComp));
            RaiseLocalEvent(ent.Owner, remEvt);
        }
    }

    private void OnLanguageShutdown(Entity<LanguageComponent> ent, ref ComponentShutdown evt)
    {
        if (TryComp<LanguageCommunicatorComponent>(ent.Comp.Holder, out var commComp) &&
            commComp.CurrentLanguage == ent)
            commComp.CurrentLanguage = null;

        foreach (var child in ent.Comp.Children)
        {
            PredictedQueueDel(child);
        }
    }

    private bool InsertLanguageAndChildren(EntityUid target,
        ProtoId<LanguagePrototype> languageProto,
        ProtoId<LanguageFluencyPrototype> fluencyProto,
        bool speaks,
        out List<Entity<LanguageComponent>> addedEntities)
    {
        addedEntities = new();
        if (!_proto.TryIndex(languageProto, out var language) || !_proto.TryIndex(fluencyProto, out var fluency))
            return false;

        var communicator = EnsureComp<LanguageCommunicatorComponent>(target);
        if (communicator.Languages is not { } languages)
            return false;

        // The client can't predict spawning entities
        if (_netMan.IsClient)
            return true;

        var entity = SpawnLanguageEntity(languageProto, fluencyProto, speaks);
        entity.Comp.Holder = target;
        if (!_container.Insert(entity.AsType(), languages))
            return false;

        addedEntities.Add(entity);
        if (fluency < _proto.Index(MaximumFluency))
        {
            return true;
        }

        var failedChild = false;
        foreach (var (relatedLang, relatedFluency) in language.RelatedLanguages)
        {
            var childEnt = SpawnLanguageEntity(relatedLang, relatedFluency, false);
            childEnt.Comp.Holder = target;

            var childComp = EnsureComp<ChildLanguageComponent>(childEnt);
            childComp.ParentLanguage = entity;
            Dirty<ChildLanguageComponent>((childEnt, childComp));

            if (!_container.Insert(childEnt.AsType(), languages))
            {
                failedChild = true;
                continue;
            }

            entity.Comp.Children.Add(childEnt);
            addedEntities.Add(childEnt);
        }

        Dirty(entity);

        return !failedChild;
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

        EntityManager.AddComponents(languageEnt, language.LanguageComponents);

        return (languageEnt, languageComp);
    }

    protected virtual void OnLanguageRemoved(Entity<LanguageCommunicatorComponent> holder, Entity<LanguageComponent> language)
    {
        // Used on the client to update the language UI.
        // LanguageAdded doesn't exist because the inserted event occurs before the components get added on the client :(
    }

    public virtual void OnLanguageUpdated(Entity<LanguageComponent?> lang)
    {
        // Used on the client to update the language UI.
    }
}
