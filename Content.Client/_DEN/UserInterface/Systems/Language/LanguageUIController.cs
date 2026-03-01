using System.Linq;
using Content.Client._DEN.Language.EntitySystems;
using Content.Client._DEN.UserInterface.Systems.Language.Controls;
using Content.Client._DEN.UserInterface.Systems.Language.Windows;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Examine;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._DEN.UserInterface.Systems.Language;

[UsedImplicitly]
public sealed class LanguageUIController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<LanguageSystem>
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    [UISystemDependency] private readonly LanguageSystem _languageSystem = default!;

    private MenuButton? LanguageButton =>
        UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.LanguageButton;

    private LanguageWindow? _window;

    public void UnloadButton()
    {
        if (LanguageButton == null)
            return;

        LanguageButton.Pressed = false;
        LanguageButton.OnPressed -= LanguageButtonPressed;
    }

    public void LoadButton()
    {
        if (LanguageButton == null)
            return;

        LanguageButton.OnPressed += LanguageButtonPressed;
    }

    private void LanguageButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        if (_window.IsOpen)
        {
            _window.Close();
            return;
        }

        _window.Open();
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (_window is { UpdateNeeded: true })
            RebuildWindow();
    }

    private void RebuildWindow()
    {
        if (_window == null)
            return;

        var player = _playerManager.LocalEntity;
        if (player == null)
            return;

        _window.UpdateNeeded = false;

        List<LanguageContainer> existingList = new(_window.LanguageList.ChildCount);
        LanguageContainer speakingContainer;
        if (_window.CurrentlySpeaking.ChildCount > 0 && _window.CurrentlySpeaking.Children.First() is LanguageContainer languageContainer)
        {
            speakingContainer = languageContainer;
        }
        else
        {
            speakingContainer = new LanguageContainer(_entities, _playerManager, _prototypeManager, _languageSystem);
            _window.CurrentlySpeaking.AddChild(speakingContainer);
        }
        speakingContainer.SetCurrentSpoken();

        foreach (var child in _window.LanguageList.Children)
        {
            if (child is LanguageContainer langContainer)
                existingList.Add(langContainer);
        }

        if (_languageSystem.TryGetLanguageEntities(player.Value, out var languages)
            && _languageSystem.GetCurrentLanguageEntity(player.Value) is { } currentLanguageEnt)
        {
            languages.Remove(currentLanguageEnt);

            speakingContainer.UpdateLanguage(currentLanguageEnt);
            Log.Debug("Building with: " + currentLanguageEnt.Comp.Language.Id);

            // Languages in the UI are sorted by their localized name, just to add some semblance of stability.
            languages.Sort((entity1, entity2) =>
            {
                var langProto1 = _prototypeManager.Index(entity1.Comp.Language);
                var langProto2 = _prototypeManager.Index(entity2.Comp.Language);

                return string.Compare(langProto1.LocalizedName, langProto2.LocalizedName, StringComparison.CurrentCulture);
            });

            var i = 0;
            foreach (var language in languages)
            {
                if (i < existingList.Count)
                {
                    existingList[i++].UpdateLanguage(language);
                    continue;
                }

                var langCont = new LanguageContainer(_entities, _playerManager, _prototypeManager, _languageSystem);
                langCont.UpdateLanguage(language);
                _window.LanguageList.AddChild(langCont);
            }

            for (; i < existingList.Count; i++)
            {
                _window.LanguageList.RemoveChild(existingList[i]);
            }
        }
    }

    private void OnLanguagesUpdated()
    {
        if (_window != null)
            _window.UpdateNeeded = true;
    }

    private void DeactivateButton()
    {
        LanguageButton?.Pressed = false;
    }

    private void ActivateButton()
    {
        LanguageButton?.Pressed = true;
    }

    public void OnStateEntered(GameplayState state)
    {
        _window = UIManager.CreateWindow<LanguageWindow>();
        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenLanguageMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<LanguageUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
        }

        CommandBinds.Unregister<LanguageUIController>();
    }

    public void OnSystemLoaded(LanguageSystem system)
    {
        system.OnLanguageUpdate += OnLanguagesUpdated;
    }

    public void OnSystemUnloaded(LanguageSystem system)
    {
        system.OnLanguageUpdate -= OnLanguagesUpdated;
    }
}
