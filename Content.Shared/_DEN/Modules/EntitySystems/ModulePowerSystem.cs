using Content.Shared._DEN.Modules.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;

namespace Content.Shared._DEN.Modules.EntitySystems;

public sealed partial class ModulePowerSystem : EntitySystem
{
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private PowerCellSystem _cell = default!;
    
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
        if (!TryComp<ModuleStorageComponent>(entity, out var storage) || storage.ModuleContainer is null)
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
        if (!TryComp<ModuleStorageComponent>(entity, out var storage) || storage.ModuleContainer is null)
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
        if (!TryComp<ModuleStorageComponent>(entity, out var storage) || storage.ModuleContainer is null)
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

    /// <summary>
    /// Attempts to use charge on the module, this involves finding the controller that is responsible for the module
    /// and applying its DrainMultiplier to the provided value.
    /// </summary>
    /// <param name="entity">The module or module power provider being operated on.</param>
    /// <param name="draw">The amount of power to drain.</param>
    /// <param name="user">Optional user trying to perform this action (passed to PowerCellSystem).</param>
    /// <param name="predicted">If the no power popup is predicted (passed to PowerCellSystem).</param>
    /// <returns>If using the charge succeeded.</returns>
    public bool TryUseCharge(EntityUid entity, float draw, EntityUid? user = null, bool predicted = false)
    {
        Entity<ModulePowerProviderComponent>? provider = null;
        
        if (TryComp<ModuleComponent>(entity, out var module))
        {
            if (module.StoredIn is null)
                return false;

            if (!TryComp<ModulePowerProviderComponent>(module.StoredIn, out var providerComp))
                return false;
            
            provider = (module.StoredIn.Value, providerComp);
        } 
        else if (TryComp<ModulePowerProviderComponent>(entity, out var providerComp))
        {
            provider = (entity, providerComp);
        }

        if (provider is null)
            return false;

        return _cell.TryUseCharge(provider.Value.Owner, draw * provider.Value.Comp.DrainMultiplier, user, predicted);
    }
    
    /// <summary>
    /// Checks if the entity has access to enough charge through ModulePowerSystem.
    /// </summary>
    /// <param name="entity">The module or module power provider being operated on.</param>
    /// <param name="draw">The amount of power being drained.</param>
    /// <param name="user">Optional user trying to perform this action (passed to PowerCellSystem).</param>
    /// <param name="predicted">If the no power popup is predicted (passed to PowerCellSystem).</param>
    /// <returns>If the charge is available.</returns>
    public bool HasCharge(EntityUid entity, float draw, EntityUid? user, bool predicted = false)
    {
        Entity<ModulePowerProviderComponent>? provider = null;
        
        if (TryComp<ModuleComponent>(entity, out var module))
        {
            if (module.StoredIn is null)
                return false;

            if (!TryComp<ModulePowerProviderComponent>(module.StoredIn, out var providerComp))
                return false;
            
            provider = (module.StoredIn.Value, providerComp);
        } 
        else if (TryComp<ModulePowerProviderComponent>(entity, out var providerComp))
        {
            provider = (entity, providerComp);
        }

        if (provider is null)
            return false;

        return _cell.HasCharge(provider.Value.Owner, draw * provider.Value.Comp.DrainMultiplier, user, predicted);
    }
}