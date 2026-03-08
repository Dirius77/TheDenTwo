using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._DEN.Language.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._DEN.Language.EntitySystems;

public abstract partial class SharedLanguageSystem
{
    private static readonly ProtoId<LanguageFluencyPrototype> DefaultLanguageFluency = "Fluent";

    /// <summary>
    ///     Sets the currently spoken language by the entity to the passed language if it speaks it.
    /// </summary>
    /// <param name="target">The entity to set the language on.</param>
    /// <param name="languageProto">The language to set the entity to.</param>
    /// <returns>Whether the language was set.</returns>
    [PublicAPI]
    public bool TrySetLanguage(EntityUid target, ProtoId<LanguagePrototype> languageProto)
    {
        if (!SpeaksLanguage(target, languageProto, out var languageEntity))
            return false;

        var communicator = EnsureComp<LanguageCommunicatorComponent>(target);

        if (communicator.CurrentLanguage == languageEntity.Value)
            return true;

        communicator.CurrentLanguage = languageEntity;
        communicator.LastSpokenLanguage = languageProto;
        Dirty<LanguageCommunicatorComponent>((target, communicator));
        return true;
    }

    /// <summary>
    ///     Tries to set the spoken language to the specified languageEntity.
    /// </summary>
    /// <param name="target">Entity to set the language on.</param>
    /// <param name="languageEntity">The language entity to try to set as the spoken language.</param>
    /// <returns></returns>
    [PublicAPI]
    public bool TrySetLanguage(EntityUid target, Entity<LanguageComponent> languageEntity)
    {
        var communicator = EnsureComp<LanguageCommunicatorComponent>(target);

        if (communicator.Languages is not { } languages)
            return false;

        if (!languages.Contains(languageEntity))
            return false;

        if (!languageEntity.Comp.Speaks)
            return false;

        if (communicator.CurrentLanguage == languageEntity)
            return true;

        communicator.CurrentLanguage = languageEntity;
        communicator.LastSpokenLanguage = languageEntity.Comp.Language;
        Dirty<LanguageCommunicatorComponent>((target, communicator));
        return true;
    }

    #region Add Methods

    /// <summary>
    ///     Adds a language entity to the target entity.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="languageEntity"></param>
    /// <returns></returns>
    public bool TryAddLanguage(EntityUid target,
        Entity<LanguageComponent> languageEntity)
    {
        var communicator = EnsureComp<LanguageCommunicatorComponent>(target);
        if (communicator.Languages is not { } languages)
            return false;

        return _container.Insert(languageEntity.Owner, languages);
    }

    /// <summary>
    ///     Adds a language to the target entity. The entity will be able to speak and fully understand the language.
    ///     This may add multiple languages if the language has related languages.
    /// </summary>
    /// <param name="target">The entity to add the language to.</param>
    /// <param name="language">The ID of the language to add.</param>
    /// <param name="languageEntities">The list of added languages.</param>
    /// <returns>Whether the operation succeeded. Note that languages may have still been added if a related language failed.</returns>
    [PublicAPI]
    public bool TryAddLanguage(EntityUid target,
        ProtoId<LanguagePrototype> language,
        out List<Entity<LanguageComponent>> languageEntities)
    {
        return TryAddLanguage(target, language,true, DefaultLanguageFluency, out languageEntities);
    }

    /// <summary>
    ///     Adds a language to the target entity.
    ///     This may add multiple languages if the language has related languages.
    /// </summary>
    /// <param name="target">The entity to add the language to.</param>
    /// <param name="languageProto">The ID of the language to add.</param>
    /// <param name="speaks">Whether the target should be able to speak the language.</param>
    /// <param name="fluencyProto">The amount of fluency the target should have with the language.</param>
    /// <param name="languageEntities">The list of added languages.</param>
    /// <returns>Whether the operation succeeded. Note that languages may have still been added if a related language failed.</returns>
    [PublicAPI]
    public bool TryAddLanguage(EntityUid target,
        ProtoId<LanguagePrototype> languageProto,
        bool speaks,
        ProtoId<LanguageFluencyPrototype> fluencyProto,
        out List<Entity<LanguageComponent>> languageEntities)
    {
        languageEntities = [];

        return InsertLanguageAndChildren(target, languageProto, fluencyProto, speaks, out languageEntities);
    }
    #endregion

    #region Remove Methods
    /// <summary>
    ///     Removes the most fluent instance of a language from an entity. This will leave less fluent instances
    ///     such as related languages.
    /// </summary>
    /// <param name="target">The entity to remove the language from.</param>
    /// <param name="languageProto">The language to remove.</param>
    /// <returns>If a language was successfully removed.</returns>
    [PublicAPI]
    public bool TryRemoveLanguage(EntityUid target, ProtoId<LanguagePrototype> languageProto)
    {
        if (!TryComp<LanguageCommunicatorComponent>(target, out var communicator))
            return false;

        if (!TryGetLanguageEntity(target, languageProto, out var languageEntity) || Deleted(languageEntity.Value))
            return false;

        if (communicator.CurrentLanguage is not null && communicator.CurrentLanguage.Value.Equals(languageEntity.Value))
        {
            communicator.CurrentLanguage = null;
            Dirty<LanguageCommunicatorComponent>((target, communicator));
        }

        PredictedQueueDel(languageEntity);
        return true;
    }

    /// <summary>
    ///     Removes ALL instances of a language from an entity, regardless of source. This may cause odd behavior with
    ///     translators or other sources of language.
    /// </summary>
    /// <param name="target">The entity to remove the language from.</param>
    /// <param name="languageProto">The language to remove.</param>
    /// <returns>If all the languages were successfully removed.</returns>
    [PublicAPI]
    public bool TryRemoveLanguages(EntityUid target, ProtoId<LanguagePrototype> languageProto)
    {
        if (!TryComp<LanguageCommunicatorComponent>(target, out var communicator))
            return false;

        if (!TryGetLanguageEntities(target, languageProto, out var languageEntities))
            return false;

        var errored = false;
        foreach (var languageEntity in languageEntities)
        {
            if (Deleted(languageEntity))
            {
                errored = true;
                continue;
            }

            if (communicator.CurrentLanguage is not null && communicator.CurrentLanguage.Value.Equals(languageEntity))
            {
                communicator.CurrentLanguage = null;
                Dirty<LanguageCommunicatorComponent>((target, communicator));
            }

            PredictedQueueDel(languageEntity);
        }

        return !errored;
    }
    #endregion

    #region Get Methods

    /// <summary>
    /// Fetches the current default language.
    /// </summary>
    /// <returns>The ProtoId of the current default language.</returns>
    [PublicAPI]
    public ProtoId<LanguagePrototype> GetDefaultLanguage()
    {
        return _defaultLanguage;
    }

    /// <summary>
    ///     Retrieves the currently spoken language of the entity. If the entity isn't currently set to one, but it
    ///     does speak one, then it will be set to the first language it speaks.
    /// </summary>
    /// <param name="target">The entity to retrieve the current language of.</param>
    /// <param name="forceDefault">Forces the creation of a default language regardless of fallback being on. This
    ///     is for use by systems/entities that need to send radio messages.</param>
    /// <returns>The language entity for the currently spoken language, or null if there are none.</returns>
    [PublicAPI]
    public Entity<LanguageComponent>? GetCurrentLanguageEntity(EntityUid target, bool forceDefault = false)
    {
        LanguageCommunicatorComponent? communicator;
        if (!TryComp(target, out communicator))
        {
            if (forceDefault || _fallbackDefaultLanguage)
            {
                InsertLanguageAndChildren(target, _defaultLanguage, DefaultLanguageFluency, true, out _);
                communicator = EnsureComp<LanguageCommunicatorComponent>(target); // Should already exist here.
            }
            else
            {
                return null;
            }
        }

        if (communicator.CurrentLanguage is null || Deleted(communicator.CurrentLanguage))
        {
            if (!TryGetLanguageEntities(target, out var languageEntities))
                return null;

            var spokenLanguages = languageEntities.FindAll(lang => lang.Comp.Speaks);
            if (communicator.LastSpokenLanguage is { } lastSpoken)
            {
                communicator.CurrentLanguage = spokenLanguages.FirstOrNull(lang => lang.Comp.Language == lastSpoken);
            }

            communicator.CurrentLanguage ??= spokenLanguages.FirstOrNull();
            if (communicator.CurrentLanguage is not null)
                Dirty(target, communicator);
        }

        if (communicator.CurrentLanguage is not null)
        {
            if (TryComp<LanguageComponent>(communicator.CurrentLanguage, out var languageComp))
            {
                return (communicator.CurrentLanguage.Value, languageComp);
            }
            // This can happen when a client reconnects mid-round.
            // The problem is only client side, so it breaks the UI for a second, everything resolves itself eventually.
            Log.Warning("Currently spoken 'language' is not a language for: " + Name(target));
        }
        return null;
    }

    /// <summary>
    ///     Retrieves the currently spoken language of the entity. If the entity isn't currently set to one, but it
    ///     does speak one, then it will be set to the first language it speaks.
    ///     If the entity does not have a LanguageCommunicatorComponent then falls back on the values of
    ///     languages.use_default_language and languages.default_language
    /// </summary>
    /// <param name="target">The entity to retrieve the current language of.</param>
    /// <returns>The language entity for the currently spoken language, or null if there are none.</returns>
    [PublicAPI]
    public ProtoId<LanguagePrototype>? GetCurrentLanguage(EntityUid target)
    {
        var languageEnt = GetCurrentLanguageEntity(target);

        return languageEnt?.Comp?.Language;
    }

    /// <summary>
    ///     Returns the first (most fluent) language entity for the given language on the target entity.
    /// </summary>
    /// <param name="target">The target entity.</param>
    /// <param name="languageProto">The language to find a language entity for.</param>
    /// <param name="languageEntity">The found language entity.</param>
    /// <returns>Whether a language entity was found.</returns>
    [PublicAPI]
    public bool TryGetLanguageEntity(EntityUid target,
        ProtoId<LanguagePrototype> languageProto,
        [NotNullWhen(true)] out Entity<LanguageComponent>? languageEntity)
    {
        languageEntity = null;

        if (!TryGetLanguageEntities(target, languageProto, out var languageEntities))
            return false;

        languageEntity = languageEntities.First();
        return true;
    }

    /// <summary>
    ///     Retrieves all the language entities from a target.
    /// </summary>
    /// <param name="target">The target entity</param>
    /// <param name="languageEntities">All the language entities on the target.</param>
    /// <returns>Whether the entities were successfully retrieved.</returns>
    [PublicAPI]
    public bool TryGetLanguageEntities(EntityUid target,
        out List<Entity<LanguageComponent>> languageEntities)
    {
        languageEntities = [];

        if (!TryComp<LanguageCommunicatorComponent>(target, out var communicator))
            return false;

        if (communicator.Languages is not { } languages)
            return false;

        languageEntities.AddRange(
            from languageEnt in languages.ContainedEntities
            where _languageQuery.HasComp(languageEnt)
            select _languageQuery.Get(languageEnt));

        return languageEntities.Count > 0;
    }

    /// <summary>
    ///     Retrieves a list of all the language entities that represent a particular language for an entity.
    /// </summary>
    /// <param name="target">The target entity to get language entities from.</param>
    /// <param name="language">The language prototype to compare against.</param>
    /// <param name="languageEntities">The language entities matching the language, sorted by fluency.</param>
    /// <returns>Whether any language entities were retrieved.</returns>
    [PublicAPI]
    public bool TryGetLanguageEntities(EntityUid target,
        ProtoId<LanguagePrototype> language,
        out List<Entity<LanguageComponent>> languageEntities)
    {
        languageEntities = [];

        if (!TryGetLanguageEntities(target, out languageEntities))
            return false;

        languageEntities = languageEntities.Where(e => e.Comp.Language == language).ToList();

        languageEntities.Sort((lhs, rhs) => rhs.Comp.Fluency.CompareTo(lhs.Comp.Fluency));

        return languageEntities.Count > 0;
    }

    /// <summary>
    ///     Retrieves a list of all the languages an entity has matching the passed prototype
    ///     as well as their fluency values and speaking state.
    /// </summary>
    /// <param name="target">The target entity to get the languages from.</param>
    /// <param name="languageProto">The language to retrieve.</param>
    /// <param name="languages">The languages found.</param>
    /// <returns>Whether any languages were retrieved.</returns>
    [PublicAPI]
    public bool TryGetLanguages(EntityUid target,
        ProtoId<LanguagePrototype> languageProto,
        out List<(ProtoId<LanguagePrototype>, ProtoId<LanguageFluencyPrototype>, bool)> languages)
    {
        languages = [];

        if (!TryGetLanguageEntities(target, languageProto, out var languageEntities))
            return false;

        languages.AddRange(languageEntities.Select(ent => (ent.Comp.Language, ent.Comp.Fluency, ent.Comp.Speaks))
            .Select(item => ((ProtoId<LanguagePrototype>, ProtoId<LanguageFluencyPrototype>, bool)) item));

        return true;
    }

    /// <summary>
    ///     Retrieves a list of all the languages an entity has, as well as their fluency values and speaking state.
    /// </summary>
    /// <param name="target">The target entity to get the languages from.</param>
    /// <param name="languages">The languages found.</param>
    /// <returns>Whether any languages were retrieved.</returns>
    [PublicAPI]
    public bool TryGetLanguages(EntityUid target,
        out List<(ProtoId<LanguagePrototype>, ProtoId<LanguageFluencyPrototype>, bool)> languages)
    {
        languages = [];

        if (!TryGetLanguageEntities(target, out var languageEntities))
            return false;

        languages.AddRange(
            languageEntities.Select(ent => (ent.Comp.Language, ent.Comp.Fluency, ent.Comp.Speaks))
            .Select(item => ((ProtoId<LanguagePrototype>, ProtoId<LanguageFluencyPrototype>, bool)) item));

        return true;
    }

    /// <summary>
    ///     Checks whether the provided entity can speak the passed language.
    /// </summary>
    /// <param name="target">The entity to check against.</param>
    /// <param name="languageProto">The language to check for.</param>
    /// <returns>Whether the entity speaks the language.</returns>
    [PublicAPI]
    public bool SpeaksLanguage(EntityUid target, ProtoId<LanguagePrototype> languageProto)
    {
        if (!TryGetLanguageEntities(target, languageProto, out var languages))
            return false;

        return languages.Exists(lang => lang.Comp.Speaks);
    }

    /// <summary>
    ///     Checks whether the provided entity can speak the passed language.
    /// </summary>
    /// <param name="target">The entity to check against.</param>
    /// <param name="languageProto">The language to check for.</param>
    /// <param name="languageEnt">The language entity responsible for this ability.</param>
    /// <returns>Whether the entity speaks the language.</returns>
    [PublicAPI]
    public bool SpeaksLanguage(EntityUid target, ProtoId<LanguagePrototype> languageProto, [NotNullWhen(true)] out Entity<LanguageComponent>? languageEnt)
    {
        languageEnt = null;

        if (!TryGetLanguageEntities(target, languageProto, out var languages))
            return false;

        languageEnt = languages.FirstOrNull(lang => lang.Comp.Speaks);

        return languageEnt != null;
    }

    /// <summary>
    ///     Checks if the provided entity understands the matching language at least as well as the provided fluency.
    /// </summary>
    /// <param name="target">The entity to check against.</param>
    /// <param name="languageProto">The language to check for.</param>
    /// <param name="minimumFluency">The minimum fluency the entity must have.</param>
    /// <returns>Whether the entity understands the language at the minimum fluency.</returns>
    [PublicAPI]
    public bool UnderstandsLanguage(EntityUid target,
        ProtoId<LanguagePrototype> languageProto,
        ProtoId<LanguageFluencyPrototype> minimumFluency)
    {
        if (!TryGetLanguageEntities(target, languageProto, out var languages))
            return false;

        return languages.Exists(lang => _proto.Index(lang.Comp.Fluency) >= _proto.Index(minimumFluency));
    }

    /// <summary>
    ///     Checks if the provided entity understands the matching language at least as well as the provided fluency.
    ///     These are sorted by fluency, so the returned entity will always be the most fluent.
    /// </summary>
    /// <param name="target">The entity to check against.</param>
    /// <param name="languageProto">The language to check for.</param>
    /// <param name="minimumFluency">The minimum fluency the entity must have.</param>
    /// <param name="languageEnt">The language entity responsible for this ability.</param>
    /// <returns>Whether the entity understands the language at the minimum fluency.</returns>
    [PublicAPI]
    public bool UnderstandsLanguage(EntityUid target,
        ProtoId<LanguagePrototype> languageProto,
        ProtoId<LanguageFluencyPrototype> minimumFluency,
        [NotNullWhen(true)] out Entity<LanguageComponent>? languageEnt)
    {
        languageEnt = null;

        if (!TryGetLanguageEntities(target, languageProto, out var languages))
            return false;

        languageEnt = languages.FirstOrNull(lang => _proto.Index(lang.Comp.Fluency) >= _proto.Index(minimumFluency));
        return languageEnt != null;
    }
    #endregion
}
