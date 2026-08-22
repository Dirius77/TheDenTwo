using System.Linq;
using Content.Client._DEN.Lobby.UI.Languages;
using Content.Shared._DEN.Language;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    
    private void RefreshLanguages()
    {
        LanguagesList.RemoveAllChildren();

        var languageEntries = _prototypeManager.EnumeratePrototypes<LanguageEntryPrototype>()
            .OrderBy(t => t.Priority)
            .ThenBy(t => _prototypeManager.Index(t.LanguageProto).LocalizedName)
            .ToList();
        
        var languageFluencies = _prototypeManager.EnumeratePrototypes<LanguageFluencyPrototype>()
            .Where(t => t.RoundStart)
            .OrderBy(t => t.Understanding)
            .ToList();

        foreach (var entry in languageEntries)
        {
            var selector = new LanguageSelector(_prototypeManager, entry, languageFluencies);
            LanguagesList.AddChild(selector);
        }
    }
}