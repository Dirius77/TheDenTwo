using System.Linq;
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
        foreach (var action in entity.Comp.Actions)
        {
            EntityUid? actionEntity = entity.Comp.ActionEntities.GetValueOrDefault(action);
            _actions.AddAction(entity.Owner, ref actionEntity, action);
            if (actionEntity is not null)
                entity.Comp.ActionEntities[action] = actionEntity.Value;
        }
        _actions.GrantContainedActions(wearer, entity.Owner);
    }
    
    private void OnClothingUnsealed(Entity<ModsuitModuleActionGrantComponent> entity,
        ref ModsuitRelayedEvent<ClothingUnsealedEvent> evt)
    {
        if (!_modsuitSystem.CanModuleBeEnabled(entity.Owner))
        {
            var wearer = Transform(evt.Owner).ParentUid;
            // Copy so removing mid-iteration doesn't error.
            var actions = entity.Comp.ActionEntities.Keys.ToList();
            foreach (var action in actions)
            {
                if (!entity.Comp.ActionEntities.Remove(action, out var actionEnt))
                    continue;
                
                _actions.RemoveAction(wearer, actionEnt);
                PredictedQueueDel(actionEnt);
            }
        }
    }
}