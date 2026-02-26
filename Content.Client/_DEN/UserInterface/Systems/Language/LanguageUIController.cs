using Content.Client._DEN.Language.EntitySystems;
using Content.Client._DEN.UserInterface.Systems.Language.Windows;
using Content.Client.Gameplay;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._DEN.UserInterface.Systems.Language;

public sealed class LanguageUIController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<LanguageSystem>
{

    private LanguageWindow? _window;

    public void OnStateEntered(GameplayState state)
    {
    }

    public void OnStateExited(GameplayState state)
    {
    }

    public void OnSystemLoaded(LanguageSystem system)
    {
    }

    public void OnSystemUnloaded(LanguageSystem system)
    {
    }
}
