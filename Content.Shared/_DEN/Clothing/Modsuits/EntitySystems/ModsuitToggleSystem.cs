using System.Linq;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Sealable.EntitySystems;
using Content.Shared._DEN.Modules.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public sealed partial class ModsuitToggleSystem : EntitySystem
{
    [Dependency] private SharedModsuitSystem _modsuitSystem = default!;
    [Dependency] private ItemToggleSystem _toggleSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<ModsuitModuleComponent, ItemToggleActivateAttemptEvent>(OnModsuitModuleToggleAttempt);
        SubscribeLocalEvent<ModsuitModuleComponent, ToggleActionEvent>(OnModsuitModuleToggleAction);
        SubscribeLocalEvent<ModsuitModuleComponent, ItemToggledEvent>(OnModsuitModuleToggled);
        
        SubscribeLocalEvent<ModsuitModuleTogglePartComponent, ItemToggledEvent>(OnModsuitModulePartToggle);
        
        SubscribeLocalEvent<ItemToggleComponent, ModsuitRelayedEvent<ClothingUnsealedEvent>>(
            OnClothingUnsealed);
        
        SubscribeLocalEvent<ModsuitModuleTogglePartComponentsComponent, ItemToggledEvent>(OnModsuitModuleTogglePartComponents);
        SubscribeLocalEvent<ModsuitModuleTogglePartComponentsComponent, ModsuitRelayedEvent<ClothingUnsealedEvent>>(OnAttachedPartUnsealed);
        SubscribeLocalEvent<ModsuitModuleTogglePartComponentsComponent, ModsuitRelayedEvent<ClothingSealedEvent>>(OnAttachedPartSealed);
        
        SubscribeLocalEvent<ModsuitModuleToggleParentComponentsComponent, ItemToggledEvent>(OnModsuitModuleToggleParentComponents);
    }

    private void OnModsuitModuleToggleAttempt(Entity<ModsuitModuleComponent> entity,
        ref ItemToggleActivateAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        
        if (!_modsuitSystem.CanModuleBeEnabled(entity.Owner))
            args.Cancelled = true;
    }

    private void OnModsuitModuleToggled(Entity<ModsuitModuleComponent> entity, ref ItemToggledEvent args)
    {
        _modsuitSystem.ModuleUpdateUI(entity.AsNullable());
    }

    private void OnAttachedPartUnsealed(Entity<ModsuitModuleTogglePartComponentsComponent> entity,
        ref ModsuitRelayedEvent<ClothingUnsealedEvent> args)
    {
        if (!_modsuitSystem.CanModuleBeEnabled(entity.Owner) 
            || !HasComp<ModsuitPartAttachedModuleComponent>(entity))
            return;
     
        if (!_modsuitSystem.PartMatchesModule(entity.Owner, args.Owner))
            return;
        
        var target = args.Owner;
        ToggleComponentsOnTarget(target, entity.Comp.AddOnToggle, entity.Comp.RemoveOnToggle, false);
    }
    
    // This exists to catch when the module doesn't have `NeedsAll`, in which case it might be activated while one
    // part is unsealed.
    private void OnAttachedPartSealed(Entity<ModsuitModuleTogglePartComponentsComponent> entity,
        ref ModsuitRelayedEvent<ClothingSealedEvent> args)
    {
        if (!_toggleSystem.IsActivated(entity.Owner))
            return;
        
        if (!TryComp<ModsuitPartAttachedModuleComponent>(entity, out var attachedComp))
            return;
        
        if (!_modsuitSystem.PartMatchesModule(entity.Owner, args.Owner))
            return;
        
        var target = args.Owner;
        ToggleComponentsOnTarget(target, entity.Comp.AddOnToggle, entity.Comp.RemoveOnToggle, true);
    }

    private void OnModsuitModuleTogglePartComponents(Entity<ModsuitModuleTogglePartComponentsComponent> entity,
        ref ItemToggledEvent args)
    {
        if (!TryComp<ModsuitPartAttachedModuleComponent>(entity, out var attachedComp))
            return;

        foreach (var target in _modsuitSystem.GetModuleAttachedParts((entity.Owner, attachedComp)))
        {
            if (!_modsuitSystem.CanPartBeActivated(target.Item2))
                continue;
            
            ToggleComponentsOnTarget(target.Item2, entity.Comp.AddOnToggle, entity.Comp.RemoveOnToggle, args.Activated);
        }
    }
    
    private void OnModsuitModuleToggleParentComponents(Entity<ModsuitModuleToggleParentComponentsComponent> entity,
        ref ItemToggledEvent args)
    {
        if (!_modsuitSystem.ModuleHasActiveController(entity.Owner, out var controller))
            return;

        var wearer = Transform(controller.Value).ParentUid;
        
        ToggleComponentsOnTarget(wearer, entity.Comp.AddOnToggle, entity.Comp.RemoveOnToggle, args.Activated);
    }
    
    private void OnClothingUnsealed(Entity<ItemToggleComponent> entity,
        ref ModsuitRelayedEvent<ClothingUnsealedEvent> args)
    {
        if (!_modsuitSystem.CanModuleBeEnabled(entity.Owner))
        {
            _toggleSystem.TryDeactivate(entity.Owner);
        }
    }

    private void OnModsuitModuleToggleAction(Entity<ModsuitModuleComponent> entity,
        ref ToggleActionEvent args)
    {
        args.Handled = _toggleSystem.Toggle(entity.Owner, args.Performer);
    }

    private void OnModsuitModulePartToggle(Entity<ModsuitModuleTogglePartComponent> entity, ref ItemToggledEvent args)
    {
        // We don't have an attached part, do nothing.
        if (!TryComp<ModsuitPartAttachedModuleComponent>(entity, out var attachedComp))
            return;
        
        // Skip checking slots if we're turning off, just in case something got unequipped before we did this.
        if (!args.Activated)
        {
            foreach (var part in _modsuitSystem.GetModuleAttachedParts((entity.Owner, attachedComp)))
            {
                _toggleSystem.TryDeactivate(part.Item2);
            }
            return;
        }

        if (!_modsuitSystem.CanModuleBeEnabled(entity.Owner))
            return;

        // Try to turn on all the relevant parts.
        foreach (var part in _modsuitSystem.GetModuleAttachedParts((entity.Owner, attachedComp)))
        {
            if (_modsuitSystem.CanPartBeActivated(part.Item2))
                _toggleSystem.TrySetActive(part.Item2, args.Activated, args.User);
        }
    }
    
    private void ToggleComponentsOnTarget(EntityUid target, ComponentRegistry? addOnToggle,
        ComponentRegistry? removeOnToggle, bool activated)
    {
        if (activated)
        {
            if (addOnToggle is not null)
                EntityManager.AddComponents(target, addOnToggle);
            
            if (removeOnToggle is not null)
                EntityManager.RemoveComponents(target, removeOnToggle);
        }
        else
        {
            if (addOnToggle is not null)
                EntityManager.RemoveComponents(target, addOnToggle);
            
            if (removeOnToggle is not null)
                EntityManager.AddComponents(target, removeOnToggle);
        }
    }
}