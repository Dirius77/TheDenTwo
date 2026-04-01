using System.Linq;
using Content.Client._DEN.Language.EntitySystems;
using Content.Client._DEN.UserInterface.Systems.Language.Controls;
using Content.Client._DEN.UserInterface.Systems.Language.Windows;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

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

    private Dictionary<EntityUid, LanguageContainer> _languageContainers = new();

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

    private void OnLanguageCommunicatorUpdated(Entity<LanguageComponent>? currentLang)
    {
        if (_window == null)
            return;

        LanguageContainer? speakingContainer = null;
        if (_window.CurrentlySpeaking.ChildCount > 0 &&
            _window.CurrentlySpeaking.Children.First() is LanguageContainer languageContainer)
        {
            speakingContainer = languageContainer;
        }

        if (speakingContainer is not null)
        {
            if (speakingContainer.LanguageEnt == currentLang)
            {
                speakingContainer.SetCurrentSpoken(true);
                return;
            }

            speakingContainer.SetCurrentSpoken(false);
            if (_window.CurrentlySpeaking.Children.Contains(speakingContainer))
                _window.CurrentlySpeaking.RemoveChild(speakingContainer);
            if (!_window.LanguageList.Children.Contains(speakingContainer))
                _window.LanguageList.AddChild(speakingContainer);
        }

        if (currentLang is { } currLangEnt)
        {
            if (_languageContainers.TryGetValue(currLangEnt, out var container))
            {
                container.SetCurrentSpoken(true);
                if (_window.LanguageList.Children.Contains(container))
                {
                    _window.LanguageList.RemoveChild(container);
                }

                if (!_window.CurrentlySpeaking.Children.Contains(container))
                {
                    _window.CurrentlySpeaking.AddChild(container);
                }
            }
            else
            {
                var newCont = new LanguageContainer(_entities, _playerManager, _prototypeManager, _languageSystem);
                newCont.UpdateLanguage(currLangEnt);
                newCont.SetCurrentSpoken(true);
                _window.CurrentlySpeaking.AddChild(newCont);
                _languageContainers.Add(currLangEnt, newCont);
            }
        }

        SortChildLanguages();
    }

    private void OnLanguageUpdated(Entity<LanguageComponent> langEnt)
    {
        // If Window is ever null and being re-created we should be doing a full rebuild anyway.
        if (_window == null)
            return;

        if (_languageContainers.TryGetValue(langEnt, out var container))
        {
            if (_languageSystem.GetLocalCommunicator() is not { } localComm)
                return;

            if (localComm.Comp.Languages is { } langs && !langs.Contains(langEnt) && _window is not null)
            {
                if(_window.LanguageList.Children.Contains(container))
                    _window.LanguageList.RemoveChild(container);
                else if(_window.CurrentlySpeaking.Children.Contains(container))
                    _window.CurrentlySpeaking.RemoveChild(container);
            }
            else
            {
                container.UpdateLanguage(langEnt);
            }
        }
        else
        {
            var newCont = new LanguageContainer(_entities, _playerManager, _prototypeManager, _languageSystem);
            newCont.UpdateLanguage(langEnt);
            _window.LanguageList.AddChild(newCont);
            _languageContainers.Add(langEnt, newCont);
        }

        SortChildLanguages();
    }

    private void SortChildLanguages()
    {
        if (_window == null)
            return;

        var children = _window.LanguageList.Children.OfType<LanguageContainer>().ToList();
        _window.LanguageList.RemoveAllChildren();
        children.Sort((p, q) =>
        {
            if (p.LanguageEnt is null)
                return -1;

            if (q.LanguageEnt is null)
                return 1;

            return string.Compare(p.LanguageEnt.Value.Comp.Language.Id,
                q.LanguageEnt.Value.Comp.Language.Id,
                StringComparison.CurrentCulture);
        });
        foreach (var child in children)
        {
            _window.LanguageList.AddChild(child);
        }
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (_window is { NeedsFullRebuild: true })
            RebuildWindow();
    }

    private void RebuildWindow()
    {
        if (_window == null)
            return;

        var player = _playerManager.LocalEntity;
        if (player == null)
            return;

        _window.NeedsFullRebuild = false;

        _languageContainers.Clear();
        _window.CurrentlySpeaking.RemoveAllChildren();
        _window.LanguageList.RemoveAllChildren();

        var speakingContainer = new LanguageContainer(_entities, _playerManager, _prototypeManager, _languageSystem);
        _window.CurrentlySpeaking.AddChild(speakingContainer);
        speakingContainer.SetCurrentSpoken(true);

        if (_languageSystem.TryGetLanguageEntities(player.Value, out var languages)
            && _languageSystem.GetCurrentLanguageEntity(player.Value) is { } currentLanguageEnt)
        {
            languages.Remove(currentLanguageEnt);

            speakingContainer.UpdateLanguage(currentLanguageEnt);
            _languageContainers.Add(currentLanguageEnt, speakingContainer);

            // Languages in the UI are sorted by their localized name, just to add some semblance of stability.
            languages.Sort((entity1, entity2) =>
            {
                var langProto1 = _prototypeManager.Index(entity1.Comp.Language);
                var langProto2 = _prototypeManager.Index(entity2.Comp.Language);

                return string.Compare(langProto1.LocalizedName, langProto2.LocalizedName, StringComparison.CurrentCulture);
            });

            foreach (var language in languages)
            {
                var langCont = new LanguageContainer(_entities, _playerManager, _prototypeManager, _languageSystem);
                langCont.UpdateLanguage(language);
                _window.LanguageList.AddChild(langCont);
                _languageContainers.Add(language, langCont);
            }
        }
    }

    private void NeedsFullRebuild()
    {
        if (_window != null)
            _window.NeedsFullRebuild = true;
    }

    private void CheckLanguageEnabled(bool enabled)
    {
        if (_window is { IsOpen: true } && !enabled)
        {
            _window.Close();
        }

        if (LanguageButton == null)
            return;

        if (enabled)
        {
            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenLanguageMenu,
                    InputCmdHandler.FromDelegate(_ => ToggleWindow()))
                .Register<LanguageUIController>();
        }
        else
        {
            CommandBinds.Unregister<LanguageUIController>();
        }
        LanguageButton.Visible = enabled;
    }

    private void OnPlayerAttached(EntityUid uid)
    {
        NeedsFullRebuild();
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

        CheckLanguageEnabled(_languageSystem.LanguagesEnabled);

        NeedsFullRebuild();
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
        system.OnLanguageEntityUpdate += OnLanguageUpdated;
        system.OnLanguageCommunicatorUpdate += OnLanguageCommunicatorUpdated;
        system.OnLanguagesEnabledUpdate += CheckLanguageEnabled;
        _playerManager.LocalPlayerAttached += OnPlayerAttached;
    }

    public void OnSystemUnloaded(LanguageSystem system)
    {
        system.OnLanguageEntityUpdate -= OnLanguageUpdated;
        system.OnLanguageCommunicatorUpdate -= OnLanguageCommunicatorUpdated;
        system.OnLanguagesEnabledUpdate -= CheckLanguageEnabled;
        _playerManager.LocalPlayerAttached -= OnPlayerAttached;

        CommandBinds.Unregister<LanguageUIController>();
    }
}
