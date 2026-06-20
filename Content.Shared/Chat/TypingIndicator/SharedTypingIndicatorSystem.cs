using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Supports typing indicators on entities.
/// </summary>
public abstract partial class SharedTypingIndicatorSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedLanguageSystem _language = default!; // DEN: Languages

    /// <summary>
    ///     Default ID of <see cref="TypingIndicatorPrototype"/>
    /// </summary>
    public static readonly ProtoId<TypingIndicatorPrototype> InitialIndicatorId = "default";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<TypingIndicatorComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<TypingIndicatorClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<TypingIndicatorClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<TypingIndicatorClothingComponent, InventoryRelayedEvent<BeforeShowTypingIndicatorEvent>>(BeforeShow);

        SubscribeAllEvent<TypingChangedEvent>(OnTypingChanged);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        // when player poses entity we want to make sure that there is typing indicator
        EnsureComp<TypingIndicatorComponent>(ev.Entity);
        // we also need appearance component to sync visual state
        EnsureComp<AppearanceComponent>(ev.Entity);
    }

    private void OnPlayerDetached(EntityUid uid, TypingIndicatorComponent component, PlayerDetachedEvent args)
    {
        // player left entity body - hide typing indicator
        SetTypingIndicatorState(uid, TypingIndicatorState.None);
    }

    private void OnGotEquipped(Entity<TypingIndicatorClothingComponent> entity, ref ClothingGotEquippedEvent args)
    {
        entity.Comp.GotEquippedTime = _timing.CurTime;
    }

    private void OnGotUnequipped(Entity<TypingIndicatorClothingComponent> entity, ref ClothingGotUnequippedEvent args)
    {
        entity.Comp.GotEquippedTime = null;
    }

    private void BeforeShow(Entity<TypingIndicatorClothingComponent> entity, ref InventoryRelayedEvent<BeforeShowTypingIndicatorEvent> args)
    {
        args.Args.TryUpdateTimeAndIndicator(entity.Comp.TypingIndicatorPrototype, entity.Comp.GotEquippedTime);
    }

    private void OnTypingChanged(TypingChangedEvent ev, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;
        if (!Exists(uid))
        {
            Log.Warning($"Client {args.SenderSession} sent TypingChangedEvent without an attached entity.");
            return;
        }

        var languageEnt = _language.GetCurrentLanguageEntity(uid.Value); // DEN: languages

        // check if this entity can speak or emote
        // Entities need a language to have an indicator.
        // See DenCCVars.FallbackDefaultLanguage if you need some weird entity to have an indicator. Or just add language speaker to the prototype.
        if (languageEnt == null || !_actionBlocker.CanEmote(uid.Value) && !_actionBlocker.CanSpeak(uid.Value, languageEnt.Value)) // DEN: languages
        {
            // nah, make sure that typing indicator is disabled
            SetTypingIndicatorState(uid.Value, TypingIndicatorState.None);
            return;
        }

        SetTypingIndicatorState(uid.Value, ev.State);
    }

    private void SetTypingIndicatorState(EntityUid uid, TypingIndicatorState state, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        _appearance.SetData(uid, TypingIndicatorVisuals.State, state, appearance);
    }
}
