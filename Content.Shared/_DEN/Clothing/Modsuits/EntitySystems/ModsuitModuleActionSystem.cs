using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Sealable.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Item.ItemToggle;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public sealed partial class ModsuitModuleActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedModsuitSystem _modsuitSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<ModsuitModuleActionGrantComponent, ModsuitRelayedEvent<ClothingSealedEvent>>(
            OnClothingSealed);
        SubscribeLocalEvent<ModsuitModuleActionGrantComponent, ModsuitRelayedEvent<ClothingUnsealedEvent>>(
            OnClothingUnsealed);
    }

    private void OnClothingSealed(Entity<ModsuitModuleActionGrantComponent> entity,
        ref ModsuitRelayedEvent<ClothingSealedEvent> evt)
    {
        if (!_modsuitSystem.CanModuleBeEnabled(entity.Owner))
            return;
        
        var wearer = Transform(evt.Owner).ParentUid;
        _actions.AddAction(entity.Owner, ref entity.Comp.ActionEntity, entity.Comp.Action);
        _actions.GrantContainedActions(wearer, entity.Owner);
    }
    
    private void OnClothingUnsealed(Entity<ModsuitModuleActionGrantComponent> entity,
        ref ModsuitRelayedEvent<ClothingUnsealedEvent> evt)
    {
        if (!_modsuitSystem.CanModuleBeEnabled(entity.Owner))
        {
            var wearer = Transform(evt.Owner).ParentUid;
            _actions.RemoveAction(wearer, entity.Comp.ActionEntity);
            PredictedQueueDel(entity.Comp.ActionEntity);
        }
    }
}