using Content.Shared._DEN.Modules.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;

namespace Content.Shared._DEN.Modules.EntitySystems;

public sealed partial class ModulePowerSystem : EntitySystem
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<ModulePowerProviderComponent, PowerCellSlotEmptyEvent>(OnPowerSlotEmpty);
        SubscribeLocalEvent<ModulePowerProviderComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<ModulePowerProviderComponent, ChargeChangedEvent>(OnChargeChanged);
        
        SubscribeLocalEvent<ModulePowerDrawComponent, RefreshModuleChargeRateEvent>(OnRefreshModuleChargeRate);

        SubscribeLocalEvent<ModuleTogglePowerDrawComponent, ItemToggledEvent>(OnModuleToggled);
        SubscribeLocalEvent<ModuleTogglePowerDrawComponent, ModulePowerDrainedEvent>(OnModulePowerDrained);
        SubscribeLocalEvent<ModuleTogglePowerDrawComponent, ItemToggleActivateAttemptEvent>(OnModulePowerToggleAttempt);
    }

    private void OnChargeChanged(Entity<ModulePowerProviderComponent> entity, ref ChargeChangedEvent args)
    {
        if (!TryComp<ModuleStorageComponent>(entity, out var storage))
            return;
        
        if (args.CurrentCharge > 0 || args.CurrentChargeRate > 0)
        {
            var powerRestoredEvent = new ModulePowerRestoredEvent();
            foreach (var ent in storage.ModuleContainer.ContainedEntities)
            {
                RaiseLocalEvent(ent, ref powerRestoredEvent);
            }
        }
    }

    private void OnRefreshChargeRate(Entity<ModulePowerProviderComponent> entity, ref RefreshChargeRateEvent args)
    {
        if (!TryComp<ModuleStorageComponent>(entity, out var storage))
            return;
        
        var refreshModuleChargeEvt = new RefreshModuleChargeRateEvent();
        foreach (var ent in storage.ModuleContainer.ContainedEntities)
        {
            RaiseLocalEvent(ent, ref refreshModuleChargeEvt);
        }
        args.NewChargeRate += refreshModuleChargeEvt.ChargeRate;
    }

    private void OnPowerSlotEmpty(Entity<ModulePowerProviderComponent> entity, ref PowerCellSlotEmptyEvent args)
    {
        if (!TryComp<ModuleStorageComponent>(entity, out var storage))
            return;

        var powerDrainedEvt = new ModulePowerDrainedEvent();
        foreach (var ent in storage.ModuleContainer.ContainedEntities)
        {
            RaiseLocalEvent(ent, ref powerDrainedEvt);
        }
    }

    private void OnRefreshModuleChargeRate(Entity<ModulePowerDrawComponent> entity,
        ref RefreshModuleChargeRateEvent args)
    {
        if (entity.Comp.Enabled)
            args.ChargeRate -= entity.Comp.DrawRate;
    }

    private void OnModuleToggled(Entity<ModuleTogglePowerDrawComponent> entity, ref ItemToggledEvent args)
    {
        if (!TryComp<ModulePowerDrawComponent>(entity, out var powerDraw))
            return;

        powerDraw.Enabled = args.Activated;
        
        if (!TryComp<ModuleComponent>(entity, out var module) || module.StoredIn is not { } storage)
            return;
        
        if (_cell.TryGetBatteryFromSlot(storage, out var battery))
            _battery.RefreshChargeRate(battery.Value.AsNullable());
    }

    private void OnModulePowerDrained(Entity<ModuleTogglePowerDrawComponent> entity, ref ModulePowerDrainedEvent args)
    {
        _toggle.TryDeactivate(entity.Owner);
    }

    private void OnModulePowerToggleAttempt(Entity<ModuleTogglePowerDrawComponent> entity,
        ref ItemToggleActivateAttemptEvent evt)
    {
        if (!TryComp<ModuleComponent>(entity, out var module) || module.StoredIn is not { } storage)
        {
            evt.Cancelled = true;
            return;
        }

        if (!TryComp<ModulePowerDrawComponent>(entity, out var powerDraw))
        {
            evt.Cancelled = true;
            return;
        }
        
        if (!_cell.HasCharge(storage, powerDraw.DrawRate, user: evt.User, predicted: true)
            || !_cell.HasCharge(storage, powerDraw.UseCharge, user: evt.User, predicted: true))
            evt.Cancelled = true;
    }
}