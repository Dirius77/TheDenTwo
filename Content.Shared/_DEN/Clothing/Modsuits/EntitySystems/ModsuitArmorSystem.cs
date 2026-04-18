using System.Security.Cryptography;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
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
    }

    private void OnCoefficientQuery(Entity<ModsuitArmorModuleComponent> ent,
        ref ModsuitRelayedEvent<InventoryRelayedEvent<CoefficientQueryEvent>> args)
    {
        var evt = args.Args.Args;

        string? slot = null;
        
        // If we care about specific parts then only set for those parts.
        if (TryComp<ModsuitPartAttachedModuleComponent>(ent, out var attached))
        {
            foreach (var kvp in _modsuitSystem.GetModuleAttachedParts((ent, attached)))
            {
                if (kvp.Item2 != args.Owner) 
                    continue;
                
                slot = kvp.Item1;
                break;
            }

            if (slot is null)
                return;
        }
        
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

        string? slot = null;
        
        // If we care about specific parts then only set for those parts.
        if (TryComp<ModsuitPartAttachedModuleComponent>(ent, out var attached))
        {
            foreach (var kvp in _modsuitSystem.GetModuleAttachedParts((ent, attached)))
            {
                if (kvp.Item2 != args.Owner) 
                    continue;
                
                slot = kvp.Item1;
                break;
            }

            if (slot is null)
                return;
        }
        
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

        string? slot = null;
        
        // If we care about specific parts then only set for those parts.s
        if (TryComp<ModsuitPartAttachedModuleComponent>(ent, out var attached))
        {
            foreach (var kvp in _modsuitSystem.GetModuleAttachedParts((ent, attached)))
            {
                if (kvp.Item2 != args.Owner) 
                    continue;
                
                slot = kvp.Item1;
                break;
            }

            if (slot is null)
                return;
        }
        
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
}