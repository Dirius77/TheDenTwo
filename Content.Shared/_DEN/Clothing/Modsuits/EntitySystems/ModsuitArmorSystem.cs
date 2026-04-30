using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public sealed partial class ModsuitArmorSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggleSystem = default!;
    [Dependency] private readonly SharedModsuitSystem _modsuitSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<ModsuitArmorModuleComponent, ModsuitRelayedEvent<InventoryRelayedEvent<CoefficientQueryEvent>>>(OnCoefficientQuery);
        SubscribeLocalEvent<ModsuitArmorModuleComponent, ModsuitRelayedEvent<InventoryRelayedEvent<DamageModifyEvent>>>(
            OnDamageModify);
        SubscribeLocalEvent<ModsuitArmorModuleComponent, ModsuitRelayedEvent<ArmorExamineEvent>>(OnArmorExamine);

        SubscribeLocalEvent<ModsuitExplosionResistanceModuleComponent,
            ModsuitRelayedEvent<InventoryRelayedEvent<GetExplosionResistanceEvent>>>(OnGetModuleExplosionResistance);
        SubscribeLocalEvent<ModsuitExplosionResistanceModuleComponent, ModsuitRelayedEvent<ArmorExamineEvent>>(OnExplosionResistanceExamine);
    }

    private void TryGetSlotFromModulePart(EntityUid ent, EntityUid part, out string? slot)
    {
        slot = null;
        if (!TryComp<ModsuitPartAttachedModuleComponent>(ent, out var attached)) 
            return;
        
        foreach (var kvp in _modsuitSystem.GetModuleAttachedParts((ent, attached)))
        {
            if (kvp.Item2 != part) 
                continue;
                
            slot = kvp.Item1;
            return;
        }
    }

    private void OnCoefficientQuery(Entity<ModsuitArmorModuleComponent> ent,
        ref ModsuitRelayedEvent<InventoryRelayedEvent<CoefficientQueryEvent>> args)
    {
        var evt = args.Args.Args;

        TryGetSlotFromModulePart(ent, args.Owner, out var slot);
        
        // If we're toggleable and we're off then don't set. ModsuitPartAttachedModuleComponent handles not enabling
        // if our parts aren't sealed.
        if (!_toggleSystem.IsActivated(ent.Owner))
            return;

        Dictionary<string, float>? armorCoefficients = null;
        if (ent.Comp.Modifiers is { } modifiers)
        {
            armorCoefficients = modifiers.Coefficients;
        }

        if (slot is not null && ent.Comp.SlotModifiers.TryGetValue(slot, out var slotModifier))
        {
            armorCoefficients = slotModifier.Coefficients;
        }

        if (armorCoefficients is null)
            return;
        
        foreach (var armorCoefficient in armorCoefficients)
        {
            evt.DamageModifiers.Coefficients[armorCoefficient.Key] = 
                evt.DamageModifiers.Coefficients.TryGetValue(armorCoefficient.Key, out var coefficient)
                    ? coefficient * armorCoefficient.Value 
                    : armorCoefficient.Value;
        }
    }

    private void OnDamageModify(Entity<ModsuitArmorModuleComponent> ent,
        ref ModsuitRelayedEvent<InventoryRelayedEvent<DamageModifyEvent>> args)
    {
        var evt = args.Args.Args;

        TryGetSlotFromModulePart(ent, args.Owner, out var slot);
        
        // If we're toggleable and we're off then don't set. ModsuitPartAttachedModuleComponent handles not enabling
        // if our parts aren't sealed.
        if (!_toggleSystem.IsActivated(ent.Owner))
            return;
        
        DamageModifierSet? damageModifierSet = null;
        if (ent.Comp.Modifiers is { } modifiers)
        {
            damageModifierSet = modifiers;
        }

        if (slot is not null && ent.Comp.SlotModifiers.TryGetValue(slot, out var slotModifier))
        {
            damageModifierSet = slotModifier;
        }

        if (damageModifierSet is null)
            return;
        
        evt.Damage = DamageSpecifier.ApplyModifierSet(evt.Damage, damageModifierSet);
    }
    
    private void OnArmorExamine(Entity<ModsuitArmorModuleComponent> ent,
        ref ModsuitRelayedEvent<ArmorExamineEvent> args)
    {
        var evt = args.Args;

        TryGetSlotFromModulePart(ent, args.Owner, out var slot);
        
        // If we're toggleable and we're off then don't set. ModsuitPartAttachedModuleComponent handles not enabling
        // if our parts aren't sealed.
        if (!_toggleSystem.IsActivated(ent.Owner))
            return;
        
        DamageModifierSet? damageModifierSet = null;
        if (ent.Comp.Modifiers is { } modifiers)
        {
            damageModifierSet = modifiers;
        }

        if (slot is not null && ent.Comp.SlotModifiers.TryGetValue(slot, out var slotModifier))
        {
            damageModifierSet = slotModifier;
        }

        if (damageModifierSet is null)
            return;
        
        evt.Msg.PushNewline();
        evt.Msg.AddMarkupOrThrow(Loc.GetString("armor-module-examine", ("entity", ent.Owner)));

        foreach (var coefficientArmor in damageModifierSet.Coefficients)
        {
            evt.Msg.PushNewline();

            var armorType = Loc.GetString("armor-damage-type-" + coefficientArmor.Key.ToLower());
            evt.Msg.AddMarkupOrThrow(Loc.GetString("armor-coefficient-value",
                ("type", armorType),
                ("value", MathF.Round((1f - coefficientArmor.Value) * 100, 1))
            ));
        }

        foreach (var flatArmor in damageModifierSet.FlatReduction)
        {
            evt.Msg.PushNewline();

            var armorType = Loc.GetString("armor-damage-type-" + flatArmor.Key.ToLower());
            evt.Msg.AddMarkupOrThrow(Loc.GetString("armor-reduction-value",
                ("type", armorType),
                ("value", flatArmor.Value)
            ));
        }
    }
    
    private void OnGetModuleExplosionResistance(Entity<ModsuitExplosionResistanceModuleComponent> ent, ref ModsuitRelayedEvent<InventoryRelayedEvent<GetExplosionResistanceEvent>> args)
    {
        var evt = args.Args.Args;

        TryGetSlotFromModulePart(ent, args.Owner, out var slot);

        float? coefficient = null;
        if (ent.Comp.DamageCoefficient is { } damageCoefficient)
        {
            coefficient = damageCoefficient;
        }
        
        if (slot is not null && ent.Comp.SlotCoefficients.TryGetValue(slot, out var slotCoefficient))
        {
            coefficient = slotCoefficient;
        }

        if (coefficient is null)
            return;
        
        evt.DamageCoefficient *= coefficient.Value;
    }
    
    private void OnExplosionResistanceExamine(Entity<ModsuitExplosionResistanceModuleComponent> ent, ref ModsuitRelayedEvent<ArmorExamineEvent> args)
    {
        var evt = args.Args;

        TryGetSlotFromModulePart(ent, args.Owner, out var slot);

        float? coefficient = null;
        if (ent.Comp.DamageCoefficient is { } damageCoefficient)
        {
            coefficient = damageCoefficient;
        }
        
        if (slot is not null && ent.Comp.SlotCoefficients.TryGetValue(slot, out var slotCoefficient))
        {
            coefficient = slotCoefficient;
        }

        if (coefficient is null)
            return;
        
        coefficient = MathF.Round((1f - coefficient.Value) * 100, 1);
        
        evt.Msg.PushNewline();
        evt.Msg.AddMarkupOrThrow(Loc.GetString("explosion-resist-module-examine", ("entity", ent.Owner)));
        evt.Msg.PushNewline();
        evt.Msg.AddMarkupOrThrow(Loc.GetString("explosion-resistance-coefficient-value", ("value", coefficient.Value)));
    }
}