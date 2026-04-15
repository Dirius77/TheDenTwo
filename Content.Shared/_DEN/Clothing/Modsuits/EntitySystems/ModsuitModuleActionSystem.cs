using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Sealable.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Item.ItemToggle;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public sealed partial class ModsuitModuleActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedModsuitSystem _modsuitSystem = default!;
    [Dependency] private readonly SealableClothingSystem _sealableSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<ModsuitModuleActionGrantComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ModsuitModuleActionGrantComponent, ModsuitRelayedEvent<ClothingSealedEvent>>(
            OnClothingSealed);
        SubscribeLocalEvent<ModsuitModuleActionGrantComponent, ModsuitRelayedEvent<ClothingUnsealedEvent>>(
            OnClothingUnsealed);
    }

    private void OnMapInit(Entity<ModsuitModuleActionGrantComponent> entity, ref MapInitEvent evt)
    {
        if (string.IsNullOrEmpty(entity.Comp.Action))
            return;
        
        _actions.AddAction(entity, ref entity.Comp.ActionEntity, entity.Comp.Action);
        _actions.SetToggled(entity.Comp.ActionEntity, _toggle.IsActivated(entity.Owner));
        Dirty(entity);
    }

    private void OnClothingSealed(Entity<ModsuitModuleActionGrantComponent> entity,
        ref ModsuitRelayedEvent<ClothingSealedEvent> evt)
    {
        // Check that we both have a controller, and have the correct part. This is done irrespective of the event
        // because we don't know what order the two might be sealed in.
        if (!_modsuitSystem.ModuleHasActiveController(entity.Owner, out var controller)
            || !_modsuitSystem.TryGetPartFromController(controller.Value, entity.Comp.Slot, out var part)
            || !_sealableSystem.IsSealed(part.Value.Owner))
            return;
        
        var wearer = Transform(controller.Value).ParentUid;
        _actions.GrantContainedActions(wearer, entity.Owner);
    }
    
    private void OnClothingUnsealed(Entity<ModsuitModuleActionGrantComponent> entity,
        ref ModsuitRelayedEvent<ClothingUnsealedEvent> evt)
    {
        // If either our controller or our part is now unsealed, remove the action.
        if (!_modsuitSystem.ModuleHasActiveController(entity.Owner, out var controller)
            || !_modsuitSystem.TryGetPartFromController(controller.Value, entity.Comp.Slot, out var part)
            || !_sealableSystem.IsSealed(part.Value.Owner))
        {
            var wearer = Transform(evt.Owner).ParentUid;
            _actions.RemoveProvidedActions(wearer, entity.Owner);
        }
    }
}