using System.Diagnostics.CodeAnalysis;
using Content.Shared._DEN.Language.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language.EntitySystems;

public abstract partial class SharedLanguageSystem : EntitySystem
{

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public static readonly ProtoId<LanguageFluencyPrototype> MaximumFluency = "Fluent";

    private EntityQuery<LanguageComponent> _languageQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageCommunicatorComponent, ComponentInit>(OnLanguageCommunicatorInit);

        _languageQuery = GetEntityQuery<LanguageComponent>();
    }

    private void OnLanguageCommunicatorInit(Entity<LanguageCommunicatorComponent> ent, ref ComponentInit evt)
    {
        ent.Comp.Languages = _container.EnsureContainer<Container>(ent, LanguageCommunicatorComponent.ContainerId);

        foreach (var (language, (speaks, fluency)) in ent.Comp.BaseLanguages)
        {
            TryAddLanguage(ent, language, speaks, fluency, out _);
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

        var entity = Spawn();
        var langComp = EnsureComp<LanguageComponent>(entity);
        langComp.Fluency = fluency;
        langComp.Language = languageProto;
        langComp.Holder = target;
        langComp.Speaks = speaks;
        if (!_container.Insert(entity, languages))
            return false;

        if (fluency < _proto.Index(MaximumFluency))
        {
            addedEntities.Add(entity);
            return true;
        }

        var failedChild = false;
        foreach (var (relatedLang, relatedFluency) in language.RelatedLanguages)
        {
            if (!_proto.TryIndex(relatedFluency, out var childFluency))
                continue;
            var childEnt = Spawn();
            var childLang = EnsureComp<LanguageComponent>(childEnt);
            childLang.Fluency = childFluency;
            childLang.Language = relatedLang;
            childLang.Speaks = false;
            childLang.Holder = target;

            if (!_container.Insert(childEnt, languages))
            {
                failedChild = true;
                continue;
            }

            langComp.Children.Add(childEnt);
            addedEntities.Add(childEnt);
        }

        return failedChild;
    }


    #region Server API
    public abstract bool TryGetMessageCachedValue(string key, string msg, [MaybeNullWhen(false)] out string value);

    public abstract void AddMessageToCache(string key, string msg, string value);

    public abstract bool TryGetWordCachedValue(ProtoId<LanguagePrototype> language,
        string word,
        [MaybeNullWhen(false)] out string value);

    public abstract void AddWordToCache(ProtoId<LanguagePrototype> language, string word, string value);

    public abstract Dictionary<string, int> GetCommonWords();
    #endregion
}
