using Content.Shared._DEN.Language.Components;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._DEN.Language.EntitySystems;

public sealed partial class TranslatorSystem : EntitySystem
{
    // TODO: If you move a translator from pockets with the UI on it flickers duplicate languages.
    // This would need some hacky timer stuff to fix. Or for movement between pockets and hands to not also include
    // an invisible "drop on the floor" step. <-- Upstream wants to fix this, if they do it'll fix this bug.

    [Dependency] private SharedLanguageSystem _language = default!;
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TranslatorComponent, EntParentChangedMessage>(OnTranslatorParentChanged);
        SubscribeLocalEvent<TranslatorComponent, EntGotInsertedIntoContainerMessage>(OnTranslatorInserted);
        SubscribeLocalEvent<TranslatorComponent, ItemToggledEvent>(OnItemToggle);
        SubscribeLocalEvent<TranslatorComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<TranslatorComponent, ImplantRelayEvent<LanguageAddedToCommunicatorEvent>>(OnLanguageAddedImplant);
        SubscribeLocalEvent<TranslatorComponent, InventoryRelayedEvent<LanguageAddedToCommunicatorEvent>>(
            OnLanguageAddedInventory);
        SubscribeLocalEvent<TranslatorComponent, HeldRelayedEvent<LanguageAddedToCommunicatorEvent>>(OnLanguageAddedHeld);
        SubscribeLocalEvent<TranslatorComponent, ImplantRelayEvent<LanguageRemovedFromCommunicatorEvent>>(
            OnLanguageRemovedImplant);
        SubscribeLocalEvent<TranslatorComponent, InventoryRelayedEvent<LanguageRemovedFromCommunicatorEvent>>(
            OnLanguageRemovedInventory);
        SubscribeLocalEvent<TranslatorComponent, HeldRelayedEvent<LanguageRemovedFromCommunicatorEvent>>(OnLanguageRemovedHeld);
        SubscribeLocalEvent<TranslatorComponent, ExaminedEvent>(OnTranslatorExamined);

        SubscribeLocalEvent<TranslatedLanguageComponent, ComponentStartup>(OnTranslatedLanguageStartup);
        SubscribeLocalEvent<TranslatedLanguageComponent, ExaminedEvent>(OnTranslatedLanguageExamined);
    }

    private void OnTranslatorExamined(Entity<TranslatorComponent> ent, ref ExaminedEvent args)
    {
        var translatingFromProto = _proto.Index(ent.Comp.RequiredLanguage);
        if (translatingFromProto is null)
            return;

        using (args.PushGroup(nameof(TranslatorComponent)))
        {
            args.PushMarkup(Loc.GetString("translator-translating-from-message", 
                ("language", translatingFromProto.LocalizedName)));

            foreach (var translatedId in ent.Comp.GrantedLanguageProtos.Keys)
            {
                if (_proto.TryIndex(translatedId, out var translatedProto))
                {
                    args.PushMarkup(Loc.GetString("translator-translated-to-message"));
                    args.PushMarkup(" - " + translatedProto.LocalizedName);
                }
            }
        }
    }

    private void OnTranslatedLanguageStartup(Entity<TranslatedLanguageComponent> ent, ref ComponentStartup args)
    {
        _language.OnLanguageUpdated(ent.AsType());
    }

    private void OnTranslatedLanguageExamined(Entity<TranslatedLanguageComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("language-sourced-from-translator"));
    }

    private void OnLanguageRemovedHeld(Entity<TranslatorComponent> ent,
        ref HeldRelayedEvent<LanguageRemovedFromCommunicatorEvent> args)
    {
        // HeldRelayedEvent doesn't tell us who's holding us :(
        if (_containerSystem.TryGetContainingContainer(ent.AsType(), out var container))
            OnLanguageRemoved(ent, container.Owner, args.Args);
    }

    private void OnLanguageRemovedInventory(Entity<TranslatorComponent> ent,
        ref InventoryRelayedEvent<LanguageRemovedFromCommunicatorEvent> evt)
    {
        OnLanguageRemoved(ent, evt.Owner, evt.Args);
    }

    private void OnLanguageRemovedImplant(Entity<TranslatorComponent> ent, ref ImplantRelayEvent<LanguageRemovedFromCommunicatorEvent> args)
    {
        OnLanguageRemoved(ent, args.ImplantedEntity, args.Args);
    }

    private void OnLanguageRemoved(Entity<TranslatorComponent> ent,
        EntityUid holder,
        LanguageRemovedFromCommunicatorEvent args)
    {
        if (ent.Comp.RequiredLanguage is { } requireLang)
        {
            if (!_language.SpeaksLanguage(holder, requireLang))
                RemoveTranslation(ent);
        }
    }

    private void OnLanguageAddedHeld(Entity<TranslatorComponent> ent,
        ref HeldRelayedEvent<LanguageAddedToCommunicatorEvent> args)
    {
        // HeldRelayedEvent doesn't tell us who's holding us :(
        if (_containerSystem.TryGetContainingContainer(ent.AsType(), out var container))
            OnLanguageAdded(ent, container.Owner, args.Args);
    }

    private void OnLanguageAddedImplant(Entity<TranslatorComponent> ent,
        ref ImplantRelayEvent<LanguageAddedToCommunicatorEvent> args)
    {
        OnLanguageAdded(ent, args.ImplantedEntity, args.Args);
    }

    private void OnLanguageAddedInventory(Entity<TranslatorComponent> ent,
        ref InventoryRelayedEvent<LanguageAddedToCommunicatorEvent> args)
    {
        OnLanguageAdded(ent, args.Owner, args.Args);
    }

    private void OnLanguageAdded(Entity<TranslatorComponent> ent, EntityUid holder, LanguageAddedToCommunicatorEvent args)
    {
        TryAddTranslation(ent, holder);
    }

    private void TryAddTranslation(Entity<TranslatorComponent> ent, EntityUid target)
    {
        if (!HasComp<LanguageCommunicatorComponent>(target))
            return;

        if (!_itemToggle.IsActivated(ent.AsType()))
            return;

        if (ent.Comp.CurrentlyGrantingTo is not null)
            return;

        if (!(ent.Comp.RequiredLanguage is { } requiredLanguage &&
              _language.SpeaksLanguage(target, requiredLanguage)))
            return;

        ent.Comp.CurrentlyGrantingTo = target;
        List<Entity<LanguageComponent>> languages = [];
        foreach (var (lang, (speaks, fluency)) in ent.Comp.GrantedLanguageProtos)
        {
            _language.TryAddLanguage(target, lang, fluency, speaks, out var newLangs);
            languages.AddRange(newLangs);
        }
        ent.Comp.GrantedLanguages.AddRange(languages);

        foreach (var language in languages)
        {
            EnsureComp<TranslatedLanguageComponent>(language);
        }
    }

    private void RemoveTranslation(Entity<TranslatorComponent> ent)
    {
        foreach (var language in ent.Comp.GrantedLanguages)
        {
            PredictedQueueDel(language);
        }
        ent.Comp.GrantedLanguages.Clear();
        ent.Comp.CurrentlyGrantingTo = null;
    }

    private void OnTranslatorInserted(Entity<TranslatorComponent> ent, ref EntGotInsertedIntoContainerMessage message)
    {
        TryAddTranslation(ent, message.Container.Owner);
    }

    private void OnItemToggle(Entity<TranslatorComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated)
        {
            if (_containerSystem.TryGetContainingContainer(ent.AsType(), out var container))
            {
                TryAddTranslation(ent, container.Owner);
            }
        }
        else
        {
            RemoveTranslation(ent);
        }
    }

    private void OnTranslatorParentChanged(Entity<TranslatorComponent> ent, ref EntParentChangedMessage message)
    {
        // Moving a translator from your pocket to your hand drops it on the floor invisibly. This stops the
        // translator from pointlessly removing the languages and breaking a ton of things if that was the case.
        Timer.Spawn(0,
            () =>
            {
                EntityUid? newHolder = null;
                if (_containerSystem.TryGetContainingContainer(ent.AsType(), out var container))
                {
                    newHolder = container.Owner;
                    if (newHolder  == ent.Comp.CurrentlyGrantingTo)
                        return;
                }

                RemoveTranslation(ent);

                if (newHolder is not null)
                    TryAddTranslation(ent, newHolder.Value);
            });
    }

    private void OnRefreshChargeRate(Entity<TranslatorComponent> ent, ref RefreshChargeRateEvent args)
    {
        if (_itemToggle.IsActivated(ent.AsType()))
            args.NewChargeRate -= ent.Comp.Wattage ?? TranslatorComponent.DefaultWattage;
    }
}
