using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Clothing.Sealable.EntitySystems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public sealed partial class ModsuitToggleSystem : EntitySystem
{
    [Dependency] private readonly SharedModsuitSystem _modsuitSystem = default!;
    [Dependency] private readonly ItemToggleSystem _toggleSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<ModsuitPartAttachedModuleComponent, ItemToggleActivateAttemptEvent>(OnModsuitModulePartToggleAttempt);
        SubscribeLocalEvent<ModsuitModuleTogglePartComponent, ItemToggledEvent>(OnModsuitModulePartToggle);
        SubscribeLocalEvent<ModsuitModuleTogglePartComponent, ToggleActionEvent>(OnModsuitModuleToggleAction);
        SubscribeLocalEvent<ItemToggleComponent, ModsuitRelayedEvent<ClothingUnsealedEvent>>(
            OnClothingUnsealed);
        
        SubscribeLocalEvent<ModsuitModuleTogglePartComponentsComponent, ItemToggledEvent>(OnModsuitModuleToggleComponents);
        SubscribeLocalEvent<ModsuitModuleTogglePartComponentsComponent, ModsuitRelayedEvent<ClothingUnsealedEvent>>(OnAttachedPartUnsealed);
        SubscribeLocalEvent<ModsuitModuleTogglePartComponentsComponent, ModsuitRelayedEvent<ClothingSealedEvent>>(OnAttachedPartSealed);
    }

    private void OnAttachedPartUnsealed(Entity<ModsuitModuleTogglePartComponentsComponent> entity,
        ref ModsuitRelayedEvent<ClothingUnsealedEvent> args)
    {
        if (!_modsuitSystem.CanModuleBeEnabled(entity.Owner) 
            || !TryComp<ModsuitPartAttachedModuleComponent>(entity, out var attachedComp))
            return;
     
        if (!attachedComp.AttachedParts.ContainsValue(args.Owner)) 
            return;
        
        var target = args.Owner;
        if (entity.Comp.AddOnToggle is {} addOnToggle)
            EntityManager.RemoveComponents(target, addOnToggle);
            
        if (entity.Comp.RemoveOnToggle is {} removeOnToggle) 
            EntityManager.AddComponents(target, removeOnToggle);
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
        
        if (!attachedComp.AttachedParts.ContainsValue(args.Owner)) 
            return;
        
        var target = args.Owner;
        if (entity.Comp.AddOnToggle is {} addOnToggle)
            EntityManager.RemoveComponents(target, addOnToggle);
            
        if (entity.Comp.RemoveOnToggle is {} removeOnToggle) 
            EntityManager.AddComponents(target, removeOnToggle);
    }

    private void OnModsuitModuleToggleComponents(Entity<ModsuitModuleTogglePartComponentsComponent> entity,
        ref ItemToggledEvent args)
    {
        if (!TryComp<ModsuitPartAttachedModuleComponent>(entity, out var attachedComp))
            return;

        foreach (var part in attachedComp.AttachedParts)
        {
            var target = part.Value;
            if (!_modsuitSystem.CanPartBeActivated(target))
                continue;
            
            if (args.Activated)
            {
                if (entity.Comp.AddOnToggle is {} addOnToggle)
                    EntityManager.AddComponents(target, addOnToggle);
            
                if (entity.Comp.RemoveOnToggle is {} removeOnToggle)
                    EntityManager.RemoveComponents(target, removeOnToggle);
            }
            else
            {
                if (entity.Comp.AddOnToggle is {} addOnToggle)
                    EntityManager.RemoveComponents(target, addOnToggle);
            
                if (entity.Comp.RemoveOnToggle is {} removeOnToggle)
                    EntityManager.AddComponents(target, removeOnToggle);
            }
        }
    }

    private void OnClothingUnsealed(Entity<ItemToggleComponent> entity,
        ref ModsuitRelayedEvent<ClothingUnsealedEvent> args)
    {
        if (!_modsuitSystem.CanModuleBeEnabled(entity.Owner))
        {
            _toggleSystem.TryDeactivate(entity.Owner);
        }
    }

    private void OnModsuitModuleToggleAction(Entity<ModsuitModuleTogglePartComponent> entity,
        ref ToggleActionEvent args)
    {
        args.Handled = _toggleSystem.Toggle(entity.Owner, args.Performer);
    }

    private void OnModsuitModulePartToggleAttempt(Entity<ModsuitPartAttachedModuleComponent> entity,
        ref ItemToggleActivateAttemptEvent args)
    {
        if (!_modsuitSystem.CanModuleBeEnabled(entity.Owner))
        {
            args.Cancelled = true;
        }
    }

    private void OnModsuitModulePartToggle(Entity<ModsuitModuleTogglePartComponent> entity, ref ItemToggledEvent args)
    {
        // We don't have an attached part, do nothing.
        if (!TryComp<ModsuitPartAttachedModuleComponent>(entity, out var attachedComp))
            return;
        
        // Skip the "Are things sealed" checks if we remember what part we have and we're turning it off.
        // This way if the part somehow gets unequipped before we can turn it off we can still find it to turn it off.
        if (!args.Activated)
        {
            foreach (var parts in attachedComp.AttachedParts)
            {
                _toggleSystem.TryDeactivate(parts.Value);
            }
            return;
        }

        if (!_modsuitSystem.CanModuleBeEnabled(entity.Owner))
            return;

        // Try to turn on all the relevant parts.
        foreach (var part in attachedComp.AttachedParts)
        {
            if (_modsuitSystem.CanPartBeActivated(part.Value))
                _toggleSystem.TrySetActive(part.Value, args.Activated, args.User);
        }
    }
}