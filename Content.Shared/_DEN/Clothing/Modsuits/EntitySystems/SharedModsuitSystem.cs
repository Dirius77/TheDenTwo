using System.Linq;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Wires;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public abstract partial class SharedModsuitSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    
    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

    private EntityQuery<ModsuitModuleComponent> _moduleQuery;
    
    public override void Initialize()
    {
        base.Initialize();

        _moduleQuery = GetEntityQuery<ModsuitModuleComponent>();
        
        SubscribeLocalEvent<ModsuitControlComponent, ComponentInit>(OnControlInit);
        SubscribeLocalEvent<ModsuitControlComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<ModsuitControlComponent, ModuleSlotActionMessage>(OnModuleSlotAction);
        SubscribeLocalEvent<ModsuitControlComponent, EntRemovedFromContainerMessage>(OnControllerEntityRemoved);
        SubscribeLocalEvent<ModsuitControlComponent, EntInsertedIntoContainerMessage>(OnControllerEntityInserted);
    }

    private void OnControlInit(Entity<ModsuitControlComponent> entity, ref ComponentInit args)
    {
        entity.Comp.ModuleContainer = _containerSystem.EnsureContainer<Container>(entity, entity.Comp.ContainerId);
        entity.Comp.ModuleSlots = new Dictionary<int, EntityUid?>();
        for (var i = 0; i < entity.Comp.MaxBusWidth; i++)
        {
            entity.Comp.ModuleSlots[i] = null;
        }
    }

    private void OnActivateInWorld(Entity<ModsuitControlComponent> entity, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (TryComp<WiresPanelComponent>(entity, out var wiresPanel))
        {
            if (!wiresPanel.Open)
            {
                args.Handled = true;
                return;
            }
        }

        _uiSystem.OpenUi(entity.Owner, ModsuitModuleUiKey.Key, args.User);
        args.Handled = true;
    }

    public bool ModuleFitsInSlot(Entity<ModsuitControlComponent> control, Entity<ModsuitModuleComponent?> module, int slot)
    {
        if (!Resolve(module, ref module.Comp))
            return false;
        
        if (slot < 0 || (slot + module.Comp.BusWidth) > control.Comp.MaxBusWidth)
            return false;
        
        for (var i = slot; i < (slot + module.Comp.BusWidth); i++)
        {
            if (control.Comp.ModuleSlots[i] != null)
                return false;
        }

        return true;
    }
    
    private void OnModuleSlotAction(Entity<ModsuitControlComponent> entity, ref ModuleSlotActionMessage args)
    {
        var player = args.Actor;

        if (!TryComp(player, out HandsComponent? handsComponent))
        {
            _popupSystem.PopupPredicted(Loc.GetString("modsuit-control-ui-on-receive-message-no-hands"), entity, player);
            return;
        }

        if (!_interactionSystem.InRangeUnobstructed(player, entity.Owner))
        {
            _popupSystem.PopupPredicted(Loc.GetString("modsuit-control-ui-on-receive-message-cannot-reach"), entity,
                player);
            return;
        }

        if (!_handsSystem.TryGetActiveItem((player, handsComponent), out var item))
            return;

        if (args.Slot >= entity.Comp.MaxBusWidth)
            return;
        
        if (TryComp<ToolComponent>(item, out var tool))
        {
            TryRemoveModule(entity, player, (item.Value, tool), args.Slot);
            return;
        }

        if (TryComp<ModsuitModuleComponent>(item, out var module))
        {
            TryInsertModule(entity, (item.Value, module), args.Slot);
        }
    }

    private void TryRemoveModule(Entity<ModsuitControlComponent> entity, EntityUid player, Entity<ToolComponent> tool,
        int slot)
    {
        if (entity.Comp.ModuleSlots[slot] is not {} module)
            return;

        if (!_toolSystem.HasQuality(tool, PryingQuality))
        {
            _popupSystem.PopupPredicted(Loc.GetString("modsuit-control-ui-on-receive-message-need-prying"), entity, player);
            return;
        }

        if (!entity.Comp.ModuleContainer.Contains(module))
        {
            Log.Debug("We have a module that isn't in our container.");
            return;
        }

        _containerSystem.Remove(module, entity.Comp.ModuleContainer);
        _handsSystem.PickupOrDrop(player, module);
        _toolSystem.PlayToolSound(tool, tool.Comp, player);

        if (_moduleQuery.TryComp(module, out var moduleComp))
        {
            for (int i = slot; i < slot + moduleComp.BusWidth; i++)
            {
                entity.Comp.ModuleSlots[i] = null;
            }
        }
        
        Dirty(entity);
        UpdateUi(entity);
    }

    private void TryInsertModule(Entity<ModsuitControlComponent> entity,
        Entity<ModsuitModuleComponent> module, int slot)
    {
        if (!ModuleFitsInSlot(entity, module.AsNullable(), slot))
            return;

        if (!_containerSystem.Insert(module.Owner, entity.Comp.ModuleContainer))
            return;
        
        for (var i = slot; i < (slot + module.Comp.BusWidth); i++)
        {
            entity.Comp.ModuleSlots[i] = module;
        }
        
        Dirty(entity);
        UpdateUi(entity);
    }

    private void OnControllerEntityInserted(Entity<ModsuitControlComponent> entity,
        ref EntInsertedIntoContainerMessage args)
    {
        if (_moduleQuery.TryComp(args.Entity, out var module))
        {
            OnModuleInserted(entity, (args.Entity, module));
        }
    }

    private void OnModuleInserted(Entity<ModsuitControlComponent> entity, Entity<ModsuitModuleComponent> module)
    {
        var controlEvt = new ModuleInsertedIntoControlEvent(module);
        RaiseLocalEvent(entity, controlEvt);

        var moduleEvt = new ModuleInsertedEvent(entity);
        RaiseLocalEvent(module, moduleEvt);
    }

    private void OnControllerEntityRemoved(Entity<ModsuitControlComponent> entity,
        ref EntRemovedFromContainerMessage args)
    {
        if (_moduleQuery.TryComp(args.Entity, out var module))
        {
            OnModuleRemoved(entity, (args.Entity, module));
        }
    }

    private void OnModuleRemoved(Entity<ModsuitControlComponent> entity, Entity<ModsuitModuleComponent> module)
    {
        var controlEvt = new ModuleRemovedFromControlEvent(module);
        RaiseLocalEvent(entity, controlEvt);

        var moduleEvt = new ModuleRemovedEvent(entity);
        RaiseLocalEvent(module, moduleEvt);
    }

    protected virtual void UpdateUi(Entity<ModsuitControlComponent> entity)
    {
    }
}

[Serializable, NetSerializable]
public enum ModsuitModuleUiKey : byte
{
    Key,
}