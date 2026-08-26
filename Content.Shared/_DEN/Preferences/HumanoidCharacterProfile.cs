using System.Linq;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared._DEN.Requirements.Managers;
using Content.Shared._DEN.Traits.Prototypes;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using JetBrains.Annotations;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    [DataField("_traitPreferences")]
    private HashSet<ProtoId<EntityTraitPrototype>> _entityTraitPreferences = new();

    /// <summary>
    /// Stores language preferences. Uses LanguageEntryPrototype so that entries can't accidentally be created for
    /// non-roundstart languages.
    /// </summary>
    [DataField("_languagePreferences")]
    private Dictionary<ProtoId<LanguageEntryPrototype>, LanguagePreference> _languagePreferences = new()
    {
        {
            SharedLanguageSystem.DefaultLanguageEntry, new LanguagePreference(SharedLanguageSystem.MaximumFluency, SpokenState.Speaks, true)
        }
    };

    /// <summary>
    /// <see cref="_entityTraitPreferences"/>
    /// </summary>
    public IReadOnlySet<ProtoId<EntityTraitPrototype>> EntityTraitPreferences => _entityTraitPreferences;

    /// <summary>
    /// <see cref="_languagePreferences"/>
    /// </summary>
    public IReadOnlyDictionary<ProtoId<LanguageEntryPrototype>, LanguagePreference> LanguagePreferences =>
        _languagePreferences;

    public HumanoidCharacterProfile(
        string name,
        string flavortext,
        string species,
        int age,
        Sex sex,
        ProtoId<EmoteSoundsPrototype> voice,
        Gender gender,
        HumanoidCharacterAppearance appearance,
        SpawnPriorityPreference spawnPriority,
        Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities,
        PreferenceUnavailableMode preferenceUnavailable,
        HashSet<ProtoId<AntagPrototype>> antagPreferences,
        HashSet<ProtoId<EntityTraitPrototype>> entityTraitPreferences,
        Dictionary<string, RoleLoadout> loadouts,
        Dictionary<ProtoId<LanguageEntryPrototype>, LanguagePreference> languagePreferences)
    {
        Name = name;
        FlavorText = flavortext;
        Species = species;
        Age = age;
        Sex = sex;
        Voice = voice;
        Gender = gender;
        Appearance = appearance;
        SpawnPriority = spawnPriority;
        _jobPriorities = jobPriorities;
        PreferenceUnavailable = preferenceUnavailable;
        _antagPreferences = antagPreferences;
        _entityTraitPreferences = entityTraitPreferences; // DEN
        _loadouts = loadouts;
        _languagePreferences = languagePreferences;

        var hasHighPrority = false;
        foreach (var (key, value) in _jobPriorities)
        {
            if (value == JobPriority.Never)
                _jobPriorities.Remove(key);
            else if (value != JobPriority.High)
                continue;

            if (hasHighPrority)
                _jobPriorities[key] = JobPriority.Medium;

            hasHighPrority = true;
        }
    }

    [PublicAPI]
    public HumanoidCharacterProfile WithEntityTraitPreference(ProtoId<EntityTraitPrototype> traitId, IPrototypeManager protoManager)
    {
        // null category is assumed to be default.
        if (!protoManager.TryIndex(traitId, out var traitProto))
            return new(this);

        var category = traitProto.Category;

        // Category not found so dump it.
        TraitCategoryPrototype? traitCategory = null;
        if (category != null && !protoManager.Resolve(category, out traitCategory))
            return new(this);

        var list = new HashSet<ProtoId<EntityTraitPrototype>>(_entityTraitPreferences) { traitId };

        if (traitCategory == null || traitCategory.MaxTraitPoints < 0)
        {
            return new(this)
            {
                _entityTraitPreferences = list,
            };
        }

        var count = 0;
        foreach (var trait in list)
        {
            if (!protoManager.TryIndex(trait, out var otherProto) ||
                otherProto.Category != traitCategory)
                continue;

            count += otherProto.Cost;
        }

        if (count > traitCategory.MaxTraitPoints && traitProto.Cost != 0)
            return new(this);

        return new(this)
        {
            _entityTraitPreferences = list,
        };
    }

    [PublicAPI]
    public HumanoidCharacterProfile WithoutEntityTraitPreference(ProtoId<EntityTraitPrototype> traitId, IPrototypeManager protoManager)
    {
        var list = new HashSet<ProtoId<EntityTraitPrototype>>(_entityTraitPreferences);
        list.Remove(traitId);

        return new(this)
        {
            _entityTraitPreferences = list,
        };
    }
    
    [PublicAPI]
    public HumanoidCharacterProfile WithLanguageFluency(ProtoId<LanguageEntryPrototype> languageEntry,
        ProtoId<LanguageFluencyPrototype> fluency)
    {
        var preferences = LanguagePreferences.ToDictionary();

        var speaks = SpokenState.None;
        var primary = false;
        if (preferences.TryGetValue(languageEntry, out var currentPrefs))
        {
            primary = currentPrefs.Primary;
            speaks = currentPrefs.Speaks;
            
            if (fluency != SharedLanguageSystem.MaximumFluency)
            {
                if (currentPrefs.Primary)
                    return new(this);

                if (currentPrefs.Speaks == SpokenState.Speaks)
                {
                    speaks = SpokenState.None;
                }
            }
        }
        
        if (fluency == SharedLanguageSystem.MinimumFluency && speaks != SpokenState.Translator)
        {
            preferences.Remove(languageEntry);
            return new(this)
            {
                _languagePreferences = preferences
            };
        }
        
        preferences[languageEntry] = new LanguagePreference(fluency, speaks, primary);

        var primaries = preferences.Where(p => p.Value.Primary).ToList();
        var foundPrimary = primaries.Count > 0;
        var totalPoints = CalculateUsedLanguagePoints(preferences);

        if (!foundPrimary || totalPoints > SharedLanguageSystem.LanguageSelectionPoints)
            return new(this);

        return new(this)
        {
            _languagePreferences = preferences
        };
    }
    
    [PublicAPI]
    public HumanoidCharacterProfile WithLanguagePrimary(ProtoId<LanguageEntryPrototype> languageEntry, bool primary)
    {
        var preferences = LanguagePreferences.ToDictionary();

        var speaks = SpokenState.None;
        var fluency = SharedLanguageSystem.MinimumFluency;
        if (preferences.TryGetValue(languageEntry, out var currentPrefs))
        {
            fluency = currentPrefs.Fluency;
            speaks = currentPrefs.Speaks;
        }

        if (primary)
        {
            foreach (var key in LanguagePreferences.Keys)
            {
                if (preferences.TryGetValue(key, out var pref))
                {
                    if (pref.Primary)
                    {
                        preferences[key] = new LanguagePreference(pref.Fluency, pref.Speaks, false);
                    }
                }
            }
            
            speaks = SpokenState.Speaks;
            fluency = SharedLanguageSystem.MaximumFluency;
        }
        
        if (fluency == SharedLanguageSystem.MinimumFluency && speaks != SpokenState.Translator)
        {
            preferences.Remove(languageEntry);
            return new(this)
            {
                _languagePreferences = preferences
            };
        }
        
        preferences[languageEntry] = new LanguagePreference(fluency, speaks, primary);
        var primaries = preferences.Where(p => p.Value.Primary).ToList();
        var totalPoints = CalculateUsedLanguagePoints(preferences);

        if (primaries.Count != 1 || totalPoints > SharedLanguageSystem.LanguageSelectionPoints)
            return new(this);

        return new(this)
        {
            _languagePreferences = preferences
        };
    }

    [PublicAPI]
    public HumanoidCharacterProfile WithLanguageSpeechPreference(ProtoId<LanguageEntryPrototype> languageEntry, SpokenState speaks)
    {
        var preferences = LanguagePreferences.ToDictionary();

        var primary = false;
        var fluency = SharedLanguageSystem.MinimumFluency;
        if (preferences.TryGetValue(languageEntry, out var currentPrefs))
        {
            fluency = currentPrefs.Fluency;
            primary = currentPrefs.Primary;
        }
        
        // Spoken languages must be fluent.
        if (speaks == SpokenState.Speaks)
        {
            fluency = SharedLanguageSystem.MaximumFluency;
        }

        if (speaks != SpokenState.Speaks)
        {
            primary = false;
        }
        
        if (fluency == SharedLanguageSystem.MinimumFluency && speaks != SpokenState.Translator)
        {
            preferences.Remove(languageEntry);
            return new(this)
            {
                _languagePreferences = preferences
            };
        }
        
        preferences[languageEntry] = new LanguagePreference(fluency, speaks, primary);

        var primaries = preferences.Where(p => p.Value.Primary).ToList();
        var foundPrimary = primaries.Count > 0;
        var totalPoints = CalculateUsedLanguagePoints(preferences);

        if (!foundPrimary || totalPoints > SharedLanguageSystem.LanguageSelectionPoints)
            return new(this);

        return new(this)
        {
            _languagePreferences = preferences
        };
    }

    [PublicAPI]
    public static int CalculateUsedLanguagePoints(
        Dictionary<ProtoId<LanguageEntryPrototype>, LanguagePreference> preferences)
    {
        var totalPoints = 0;
        foreach (var preference in preferences)
        {
            if (preference.Value.Speaks != SpokenState.None)
                totalPoints += 1;

            if (preference.Value.Fluency == SharedLanguageSystem.MaximumFluency)
                totalPoints += 1;
            
            if (preference.Value.Fluency != SharedLanguageSystem.MinimumFluency)
                totalPoints += 1;
        }

        return totalPoints;
    }

    [PublicAPI]
    public int CalculateUsedLanguagePoints()
    {
        return CalculateUsedLanguagePoints(_languagePreferences);
    }

    /// <summary>
    /// Takes in an IEnumerable of traits and returns a List of the valid traits.
    /// </summary>
    public List<ProtoId<EntityTraitPrototype>> GetValidEntityTraits(IEnumerable<ProtoId<EntityTraitPrototype>> traits,
        IPrototypeManager protoManager)
    {
        // Track points count for each group.
        var groups = new Dictionary<string, int>();
        var result = new List<ProtoId<EntityTraitPrototype>>();

        foreach (var trait in traits)
        {
            if (!protoManager.TryIndex(trait, out var traitProto))
                continue;

            // Always valid.
            if (traitProto.Category == null)
            {
                result.Add(trait);
                continue;
            }

            // No category so dump it.
            if (!protoManager.Resolve(traitProto.Category, out var category))
                continue;

            var existing = groups.GetOrNew(category.ID);
            existing += traitProto.Cost;

            // Too expensive.
            if (existing > category.MaxTraitPoints)
                continue;

            groups[category.ID] = existing;
            result.Add(trait);
        }

        return result;
    }

    public Dictionary<ProtoId<LanguageEntryPrototype>, LanguagePreference> EnsureValidLanguages(
        Dictionary<ProtoId<LanguageEntryPrototype>, LanguagePreference> languagePreferences,
        PlayerRequirementContext context,
        IPrototypeManager protoManager)
    {
        List<ProtoId<LanguageEntryPrototype>> invalidPrefs = [];
        foreach (var langPref in languagePreferences)
        {
            if (!protoManager.TryIndex(langPref.Key, out var langEntry) ||
                !SharedPlayerRequirementManager.CheckRequirements(context, langEntry.Requirements) ||
                (langPref.Value.Speaks == SpokenState.Speaks && !SharedPlayerRequirementManager.CheckRequirements(context, langEntry.SpeakingRequirements)))
                invalidPrefs.Add(langPref.Key);
        }

        foreach (var invalidPref in invalidPrefs)
        {
            languagePreferences.Remove(invalidPref);
        }

        var primaries = languagePreferences.Where(p => p.Value.Primary).ToList();
        if (primaries.Count > 1)
        {
            foreach (var otherPrimary in primaries[1..])
            {
                languagePreferences[otherPrimary.Key] =
                    new LanguagePreference(otherPrimary.Value.Fluency, otherPrimary.Value.Speaks, false);
            }
        }
        
        var totalPoints = CalculateUsedLanguagePoints(languagePreferences);
        
        if (totalPoints <= SharedLanguageSystem.LanguageSelectionPoints)
        {
            return languagePreferences;
        }

        // I'm not writing some clever solver for trying to find the minimum valid set of languages from a larger set,
        // just fall back to the default language.
        return new Dictionary<ProtoId<LanguageEntryPrototype>, LanguagePreference>()
        {
            {
                SharedLanguageSystem.DefaultLanguageEntry,
                new LanguagePreference(SharedLanguageSystem.MaximumFluency, SpokenState.Speaks, true)
            }
        };
    }

}
