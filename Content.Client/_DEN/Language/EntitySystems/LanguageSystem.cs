using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client._DEN.Language.EntitySystems;

public sealed class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public event Action? OnLanguageUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageComponent, AfterAutoHandleStateEvent>(OnLanguageComponentHandleState);
        SubscribeLocalEvent<LanguageCommunicatorComponent, AfterAutoHandleStateEvent>(OnLanguageCommunicatorHandleState);
    }

    public void TrySetSpokenLanguage(Entity<LanguageComponent> lang)
    {
        if (_playerManager.LocalEntity is null )
            return;

        var request = new RequestSetSpokenLanguageEvent(GetNetEntity(lang));
        RaisePredictiveEvent(request);
    }

    private void OnLanguageComponentHandleState(Entity<LanguageComponent> ent, ref AfterAutoHandleStateEvent evt)
    {
        LanguageUpdated(ent);
    }

    private void OnLanguageCommunicatorHandleState(Entity<LanguageCommunicatorComponent> ent,
        ref AfterAutoHandleStateEvent evt)
    {
        if (_playerManager.LocalEntity == ent)
            OnLanguageUpdate?.Invoke();
    }

    protected override void OnLanguageRemoved(Entity<LanguageCommunicatorComponent> holder, Entity<LanguageComponent> language)
    {
        LanguageUpdated(language);
    }

    public override void OnLanguageUpdated(Entity<LanguageComponent?> lang)
    {
        if (!Resolve(lang, ref lang.Comp))
            return;

        LanguageUpdated((lang, lang.Comp));
    }

    private void LanguageUpdated(Entity<LanguageComponent> ent)
    {
        if (_playerManager.LocalEntity == ent.Comp.Holder)
            OnLanguageUpdate?.Invoke();
    }
}
