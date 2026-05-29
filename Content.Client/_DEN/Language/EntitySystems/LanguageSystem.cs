using Content.Shared._DEN.CCVar;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared.GameTicking;
using Robust.Client.Player;

namespace Content.Client._DEN.Language.EntitySystems;

public sealed class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private IPlayerManager _playerManager = default!;

    public event Action<Entity<LanguageComponent>>? OnLanguageEntityUpdate;
    public event Action<Entity<LanguageComponent>?>? OnLanguageCommunicatorUpdate;
    public event Action<bool>? OnLanguagesEnabledUpdate;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(DenCCVars.HideLanguageFonts, SetHideLanguageFonts);
        _cfg.OnValueChanged(DenCCVars.LanguageEnabled, SetLanguageEnabledState);

        SubscribeLocalEvent<LanguageComponent, AfterAutoHandleStateEvent>(OnLanguageComponentHandleState);
        SubscribeLocalEvent<LanguageCommunicatorComponent, AfterAutoHandleStateEvent>(OnLanguageCommunicatorHandleState);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void SetLanguageEnabledState(bool enabled)
    {
        OnLanguagesEnabledUpdate?.Invoke(enabled);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent evt)
    {
        RaiseNetworkEvent(new HideFontsMessage(_cfg.GetCVar(DenCCVars.HideLanguageFonts)));
    }

    private void SetHideLanguageFonts(HideLanguageFontSetting hide)
    {
        RaiseNetworkEvent(new HideFontsMessage(hide));
    }

    /// <summary>
    /// Attempts to set the local player's spoken language to the passed language entity.
    /// </summary>
    /// <param name="lang">The language entity to set.</param>
    public void TrySetSpokenLanguage(Entity<LanguageComponent> lang)
    {
        if (_playerManager.LocalEntity is not { } localEnt ||
            !TryComp<LanguageCommunicatorComponent>(localEnt, out var localComm))
            return;

        var request = new RequestSetSpokenLanguageEvent(GetNetEntity(lang));
        RaiseNetworkEvent(request);

        OnLanguageCommunicatorUpdate?.Invoke(lang);
    }

    private void OnLanguageComponentHandleState(Entity<LanguageComponent> ent, ref AfterAutoHandleStateEvent evt)
    {
        LanguageUpdated(ent);
    }

    private void OnLanguageCommunicatorHandleState(Entity<LanguageCommunicatorComponent> ent,
        ref AfterAutoHandleStateEvent evt)
    {
        if (_playerManager.LocalEntity == ent)
        {
            var currLang = GetCurrentLanguageEntity(ent);
            OnLanguageCommunicatorUpdate?.Invoke(currLang);
        }
    }

    protected override void OnLanguageRemoved(Entity<LanguageCommunicatorComponent> holder, Entity<LanguageComponent> language)
    {
        if (_playerManager.LocalEntity == holder)
        {
            OnLanguageEntityUpdate?.Invoke(language);
        }
    }

    public Entity<LanguageCommunicatorComponent>? GetLocalCommunicator()
    {
        if (_playerManager.LocalEntity is { } localEnt && TryComp<LanguageCommunicatorComponent>(localEnt, out var localCommunicator))
            return (localEnt,  localCommunicator);

        return null;
    }

    public override void OnLanguageUpdated(Entity<LanguageComponent?> lang)
    {
        if (!Resolve(lang, ref lang.Comp))
            return;

        LanguageUpdated((lang, lang.Comp));
    }

    private void LanguageUpdated(Entity<LanguageComponent> ent)
    {
        if (_playerManager.LocalEntity is { } localEnt && localEnt == ent.Comp.Holder)
        {
            OnLanguageEntityUpdate?.Invoke(ent);
        }
    }
}
