using System.Linq;
using Content.Client._DEN.Language.EntitySystems;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client._DEN.UserInterface.Systems.Language;

[UsedImplicitly]
public sealed class LanguageQuickMenuController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<LanguageSystem>
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [UISystemDependency] private readonly LanguageSystem _languageSystem = default!;

    private SimpleRadialMenu? _menu;

    private void ToggleMenu()
    {
        if (_menu is { IsOpen: true })
        {
            _menu?.Close();
        }
        else if (_menu is not null && _playerManager.LocalEntity is {} player)
        {
            var buttons = BuildLanguageButtons(player);

            _menu.SetButtons(buttons ?? []);
            _menu.OpenOverMouseScreenPosition();
        }
    }

    private IEnumerable<RadialMenuOptionBase>? BuildLanguageButtons(EntityUid player)
    {
        if (!_languageSystem.TryGetSpokenLanguageEntities(player, out var languages))
            return null;

        var languageButtons = languages.Select(language =>
        {
            var langProto = _prototypeManager.Index(language.Comp.Language);
            return new RadialMenuActionOption<Entity<LanguageComponent>>(OnLanguageChosen, language)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(langProto.Icon),
                ToolTip = langProto.LocalizedName,
            };
        });
        return languageButtons;

    }

    private void OnLanguageChosen(Entity<LanguageComponent> language)
    {
        _languageSystem.TrySetSpokenLanguage(language);
    }

    private void CheckLanguageEnabled(bool enabled)
    {
        if (_menu is { IsOpen: true } && !enabled)
        {
            _menu.Close();
            _menu = null;
        }

        if (enabled)
        {
            if (_menu is null)
                _menu = UIManager.CreateWindow<SimpleRadialMenu>();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenQuickLanguageMenu,
                    InputCmdHandler.FromDelegate(_ => ToggleMenu()))
                .Register<LanguageQuickMenuController>();
        }
        else
        {
            CommandBinds.Unregister<LanguageQuickMenuController>();
        }
    }

    public void OnStateEntered(GameplayState state)
    {
        CheckLanguageEnabled(_languageSystem.LanguagesEnabled);
    }

    public void OnStateExited(GameplayState state)
    {
        if (_menu != null)
        {
            _menu.Close();
            _menu = null;
        }

        CommandBinds.Unregister<LanguageQuickMenuController>();
    }

    public void OnSystemLoaded(LanguageSystem system)
    {
        system.OnLanguagesEnabledUpdate += CheckLanguageEnabled;
    }

    public void OnSystemUnloaded(LanguageSystem system)
    {
        system.OnLanguagesEnabledUpdate -= CheckLanguageEnabled;
    }
}
