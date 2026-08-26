using System.Linq;
using Content.Client._DEN.Lobby.UI.Languages;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.EntitySystems;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    
    private void RefreshLanguages()
    {
        LanguagesList.RemoveAllChildren();

        var languageEntries = _prototypeManager.EnumeratePrototypes<LanguageEntryPrototype>()
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => _prototypeManager.Index(t.LanguageProto).LocalizedName)
            .ToList();
        
        var languageFluencies = _prototypeManager.EnumeratePrototypes<LanguageFluencyPrototype>()
            .Where(t => t.RoundStart)
            .OrderBy(t => t.Understanding)
            .ToList();

        var points = Profile?.CalculateUsedLanguagePoints();

        LanguagePoints.AddStyleClass("LabelHeading");
        LanguagePoints.Text = $"Language Points: {points}/{SharedLanguageSystem.LanguageSelectionPoints}";
        
        foreach (var entry in languageEntries)
        {
            // Use SharedLanguageSystem for now, maybe have traits to contribute later.
            // The entire points system is very placeholder.
            var selector = new LanguageSelector(Profile, entry, languageFluencies, points, SharedLanguageSystem.LanguageSelectionPoints);
            selector.OnPreferenceUpdated += profile =>
            {
                Profile = profile;
                SetDirty();
                RefreshLanguages();
            };
            LanguagesList.AddChild(selector);
        }
    }
}