using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._DEN.Modules.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Modules.EntitySystems;

public abstract partial class SharedModuleStorageSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private SharedToolSystem _toolSystem = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;
    [Dependency] private EntityQuery<ModuleComponent> _moduleQuery = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<ModuleStorageComponent, ComponentInit>(OnModultStorageInit);
        SubscribeLocalEvent<ModuleStorageComponent, ActivateInWorldEvent>(OnActivateInWorld, before: [typeof(SharedStorageSystem)]);
        SubscribeLocalEvent<ModuleStorageComponent, ModuleSlotActionMessage>(OnModuleSlotAction);
        SubscribeLocalEvent<ModuleStorageComponent, EntRemovedFromContainerMessage>(OnStorageEntityRemoved);
        SubscribeLocalEvent<ModuleStorageComponent, EntInsertedIntoContainerMessage>(OnStorageEntityInserted);
        SubscribeLocalEvent<ModuleStorageComponent, ContainerIsInsertingAttemptEvent>(OnIsInsertingAttempt);
        SubscribeLocalEvent<ModuleStorageComponent, GetVerbsEvent<ActivationVerb>>(AddUiVerb);
        
        InitializeUILimits();
    }

    private void OnIsInsertingAttempt(Entity<ModuleStorageComponent> entity, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container != entity.Comp.ModuleContainer)
            return;

        if (!_moduleQuery.TryComp(args.EntityUid, out var moduleComp))
        {
            args.Cancel();
            return;
        }
        
        var module = (args.EntityUid, moduleComp);

        // The module is already in our storage list, likely from being assigned a slot as part of UI based insertion.
        if (entity.Comp.ModuleSlots.ContainsValue(args.EntityUid))
            return;
        
        // If the module is bigger than the BusWidth it will never fit.
        if (moduleComp.BusWidth > entity.Comp.MaxBusWidth)
        {
            args.Cancel();
            return;
        }

        // At this point, if there were no other modules, it would fit.
        if (args.AssumeEmpty)
            return;

        // If we can't find a slot that it fits in it can't be inserted.
        if (!TryFindAvailableSlot(entity, module, out var slot) || !ModuleFitsInStorage(entity, module, slot.Value))
        {
            args.Cancel();
        }
    }

    private void OnModultStorageInit(Entity<ModuleStorageComponent> entity, ref ComponentInit args)
    {
        entity.Comp.ModuleContainer = _containerSystem.EnsureContainer<Container>(entity, entity.Comp.ContainerId);
        // Point Light my behated...
        entity.Comp.ModuleContainer.OccludesLight = false;
    }

    private void OnActivateInWorld(Entity<ModuleStorageComponent> entity, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        var attemptOpenEvt = new ModuleStorageUIOpenAttemptEvent(args.User);
        RaiseLocalEvent(entity, attemptOpenEvt);
        if (attemptOpenEvt.Cancelled)
            return;

        _uiSystem.OpenUi(entity.Owner, ModuleUiKey.Key, args.User);
        args.Handled = true;
    }

    // This is separated out because it is used internally to find open slots. TryFindAvailableSlot uses this instead of
    // ModuleFitsInStorage in order to avoid spamming attempt events on every slot in the storage.
    private bool ModuleFitsInSlot(Entity<ModuleStorageComponent> storage, Entity<ModuleComponent?> module, int slot)
    {
        if (!Resolve(module, ref module.Comp))
            return false;
        
        if (slot < 0 || (slot + module.Comp.BusWidth) > storage.Comp.MaxBusWidth)
            return false;
        
        for (var i = slot; i < (slot + module.Comp.BusWidth); i++)
        {
            if (storage.Comp.ModuleSlots.TryGetValue(i, out var result) && result is not null)
                return false;
        }

        return true;
    }

    private bool PassesAllWhitelists(Entity<ModuleStorageComponent> storage, Entity<ModuleComponent> module)
    {
        if (!_whitelistSystem.IsWhitelistPassOrNull(storage.Comp.Whitelist, module))
            return false;
        
        var passes = true;
        var siblingWhitelist = module.Comp.SiblingWhitelist;
        // We have a whitelist we need to pass, so default to false.
        if (siblingWhitelist is not null)
            passes = false;
        var siblingBlacklist = module.Comp.SiblingBlacklist;
        foreach (var sibling in storage.Comp.ModuleContainer.ContainedEntities)
        {
            if (_whitelistSystem.IsWhitelistPassOrNull(siblingWhitelist, sibling))
                passes = true;
            
            if (!_whitelistSystem.IsWhitelistFailOrNull(siblingBlacklist, sibling))
                return false;
        }

        // Entity passed the blacklist.
        if (!_whitelistSystem.IsWhitelistFailOrNull(storage.Comp.Blacklist, module))
            passes = false;

        return passes;
    }
    
    private void OnModuleSlotAction(Entity<ModuleStorageComponent> entity, ref ModuleSlotActionMessage args)
    {
        var player = args.Actor;

        // Gotta have hands to do anything meaningful with module storage.
        if (!TryComp(player, out HandsComponent? handsComponent))
        {
            _popupSystem.PopupPredicted(Loc.GetString("module-storage-ui-on-receive-message-no-hands"), entity, player);
            return;
        }

        // Gotta be able to reach it as well.
        if (!_interactionSystem.InRangeUnobstructed(player, entity.Owner))
        {
            _popupSystem.PopupPredicted(Loc.GetString("module-storage-ui-on-receive-message-cannot-reach"), entity,
                player);
            return;
        }
        
        // If we have nothing in our hands, check to make sure we don't need a tool.
        if (!_handsSystem.TryGetActiveItem((player, handsComponent), out var item))
        {
            if (entity.Comp.RemovalQuality is null)
                TryRemoveModule(entity.AsNullable(), player, null, args.Slot, out _);

            return;
        }

        if (args.Slot >= entity.Comp.MaxBusWidth)
            return;
        
        // Tools remove things from storage.
        if (TryComp<ToolComponent>(item, out var tool))
        {
            TryRemoveModule(entity.AsNullable(), player, (item.Value, tool), args.Slot, out _);
            return;
        }

        // Modules get put into storage.
        if (TryComp<ModuleComponent>(item, out var module))
        {
            TryInsertModule(entity.AsNullable(), (item.Value, module), player, args.Slot);
        }
    }

    private bool TryFindAvailableSlot(Entity<ModuleStorageComponent> entity, Entity<ModuleComponent> module,
        [NotNullWhen(true)] out int? slot)
    {
        slot = null;
        
        for (int i = 0; i < entity.Comp.MaxBusWidth; i++)
        {
            if (ModuleFitsInSlot(entity, module.AsNullable(), i))
            {
                slot = i;
                return true;
            }
        }

        return false;
    }

    private void ClearSlot(Entity<ModuleStorageComponent> entity, int slot, int width)
    {
        for (int i = slot; i < slot + width; i++)
        {
            entity.Comp.ModuleSlots[i] = null;
        }

        Dirty(entity);
    }

    private void AssignModuleToSlot(Entity<ModuleStorageComponent> entity, Entity<ModuleComponent> module, int slot)
    {
        for (int i = slot; i < slot + module.Comp.BusWidth; i++)
        {
            entity.Comp.ModuleSlots[i] = module.Owner;
        }
        Dirty(entity);
    }

    private void OnStorageEntityInserted(Entity<ModuleStorageComponent> entity,
        ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container == entity.Comp.ModuleContainer && _moduleQuery.TryComp(args.Entity, out var module))
        {
            OnModuleInserted(entity, (args.Entity, module));
        }
    }

    private void OnModuleInserted(Entity<ModuleStorageComponent> entity, Entity<ModuleComponent> module)
    {
        if (!entity.Comp.ModuleSlots.Values.Contains(module.AsNullable()))
        {
            if (!TryFindAvailableSlot(entity, module, out var slot))
            {
                _containerSystem.Remove(module.Owner, entity.Comp.ModuleContainer, force: true);
                return;
            }
            AssignModuleToSlot(entity, module, slot.Value);
        }

        module.Comp.StoredIn = entity.Owner;
        
        var controlEvt = new ModuleInsertedIntoStorageEvent(module);
        RaiseLocalEvent(entity, controlEvt);

        var moduleEvt = new ModuleInsertedEvent(entity);
        RaiseLocalEvent(module, moduleEvt);
        
        Dirty(module);
        UpdateUi(entity);
    }

    private void OnStorageEntityRemoved(Entity<ModuleStorageComponent> entity,
        ref EntRemovedFromContainerMessage args)
    {
        if (args.Container == entity.Comp.ModuleContainer && _moduleQuery.TryComp(args.Entity, out var module))
        {
            OnModuleRemoved(entity, (args.Entity, module));
        }
    }

    private void OnModuleRemoved(Entity<ModuleStorageComponent> entity, Entity<ModuleComponent> module)
    {
        module.Comp.StoredIn = null;
        
        var controlEvt = new ModuleRemovedFromStorageEvent(module);
        RaiseLocalEvent(entity, controlEvt);

        var moduleEvt = new ModuleRemovedEvent(entity);
        RaiseLocalEvent(module, moduleEvt);
        
        UpdateUi(entity);
    }

    private void AddUiVerb(Entity<ModuleStorageComponent> entity, ref GetVerbsEvent<ActivationVerb> evt)
    {
        var args = evt;
        
        var attemptOpenEvt = new ModuleStorageUIOpenAttemptEvent(args.User);
        RaiseLocalEvent(entity, attemptOpenEvt);
        if (attemptOpenEvt.Cancelled)
            return;
        
        var uiOpen = _uiSystem.IsUiOpen(entity.Owner, ModuleUiKey.Key, args.User);

        ActivationVerb verb = new()
        {
            Act = () =>
            {
                if (uiOpen)
                {
                    _uiSystem.CloseUi(entity.Owner, ModuleUiKey.Key, args.User);
                }
                else
                {
                    _uiSystem.OpenUi(entity.Owner, ModuleUiKey.Key, args.User);
                }
            },
            Text = Loc.GetString(uiOpen ? "module-storage-verb-close-storage" : "module-storage-verb-open-storage")
        };

        args.Verbs.Add(verb);
    }

    protected virtual void UpdateUi(Entity<ModuleStorageComponent> entity)
    {
    }
    
    #region PublicAPI

    /// <summary>
    /// Attempts to remove the specified module from the modsuit, placing it into the user's hands if possible, or
    /// on the ground otherwise.
    /// </summary>
    /// <param name="entity">The module storage to remove from.</param>
    /// <param name="user">The user attempting to remove the module, if there is one.</param>
    /// <param name="heldTool">The tool being used to remove the module, if there is one.</param>
    /// <param name="slot">The slot to try removing from.</param>
    /// <param name="removedModule">The module that was removed.</param>
    /// <param name="ignoreTools">Whether to skip tool checks and just remove the module.</param>
    /// <returns>Whether a module was removed.</returns>
    public bool TryRemoveModule(Entity<ModuleStorageComponent?> entity, EntityUid? user, Entity<ToolComponent>? heldTool,
        int slot, [NotNullWhen(true)] out EntityUid? removedModule, bool ignoreTools = false)
    {
        removedModule = null;
        if (!Resolve(entity, ref entity.Comp))
            return false;
        
        // There's nothing in this slot, so nothing to do.
        if (entity.Comp.ModuleSlots.GetValueOrDefault(slot) is not {} module)
            return false;

        if (HasComp<UnremoveableModuleComponent>(entity))
        {
            _popupSystem.PopupPredicted(Loc.GetString("module-storage-ui-module-unremoveable"), entity, user);
            return false;
        }

        if (!ignoreTools)
        {
            if (entity.Comp.RemovalQuality is {} quality 
                && heldTool is {} tool
                && !_toolSystem.HasQuality(tool, quality))
            {
                var resolvedQuality = _protoManager.Index(quality);
                _popupSystem.PopupPredicted(Loc.GetString("module-storage-ui-on-receive-message-need-tool-quality", ("quality", resolvedQuality.Name)), entity, user);
                return false;
            }
        }

        if (!entity.Comp.ModuleContainer.Contains(module))
        {
            Log.Debug("We have a module that isn't in our container.");
            return false;
        }

        _containerSystem.Remove(module, entity.Comp.ModuleContainer);
        _handsSystem.PickupOrDrop(user, module);
        // Only play the sound if we actually used a tool.
        if (heldTool is not null && entity.Comp.RemovalQuality is not null)
            _toolSystem.PlayToolSound(heldTool.Value, heldTool.Value.Comp, user);

        if (_moduleQuery.TryComp(module, out var moduleComp))
        {
            ClearSlot((entity, entity.Comp), slot, moduleComp.BusWidth);
        }
        
        removedModule = module;
        Dirty(entity);
        UpdateUi((entity, entity.Comp));
        return true;
    }
    
    /// <summary>
    /// Attempts to insert the provided module into the provided storage.
    /// </summary>
    /// <param name="entity">The storage to insert into</param>
    /// <param name="module"></param>
    /// <param name="player"></param>
    /// <param name="slot"></param>
    /// <returns>Whether insertion succeeded.</returns>
    public bool TryInsertModule(Entity<ModuleStorageComponent?> entity,
        Entity<ModuleComponent?> module, EntityUid? player, int slot)
    {
        if (!Resolve(entity, ref entity.Comp) || !Resolve(module, ref module.Comp))
            return false;
        
        if (!ModuleFitsInStorage((entity, entity.Comp), module.AsNullable(), slot))
            return false;

        AssignModuleToSlot((entity, entity.Comp), (module, module.Comp), slot);
        
        if (!_containerSystem.Insert(module.Owner, entity.Comp.ModuleContainer))
        {
            
            return false;
        }
        
        if (entity.Comp.InsertSound is { } insertSound && player is not null)
            _audioSystem.PlayPredicted(insertSound, entity, player);
        
        Dirty(entity);
        UpdateUi((entity, entity.Comp));
        return true;
    }
    
    /// <summary>
    /// Checks to see if a module fits in a slot within a storage, including raising events and passing whitelists.
    /// </summary>
    /// <param name="storage">The storage to check</param>
    /// <param name="module">The module to check</param>
    /// <param name="slot">The slot in the storage to check</param>
    /// <returns>Whether the module fits in the slot.</returns>
    public bool ModuleFitsInStorage(Entity<ModuleStorageComponent> storage, Entity<ModuleComponent?> module, int slot)
    {
        if (!Resolve(module, ref module.Comp, false))
            return false;

        if (!ModuleFitsInSlot(storage, module, slot))
            return false;

        if (!PassesAllWhitelists(storage, (module, module.Comp)))
            return false;
        
        // Raised on the module itself to allow it to reject the insertion.
        var moduleInsertAttempt = new ModuleGettingInsertedAttemptEvent(storage);
        RaiseLocalEvent(module, moduleInsertAttempt);
        if (moduleInsertAttempt.Cancelled)
            return false;

        // Raised on the storage to allow it to reject the insertion.
        var storageInsertAttempt = new AttemptInsertModuleIntoStorageEvent((module, module.Comp));
        RaiseLocalEvent(storage, storageInsertAttempt);
        return !storageInsertAttempt.Cancelled;
    }
    
    /// <summary>
    /// Returns the list of modules contained within the storage.
    /// </summary>
    /// <param name="storage">The storage entity</param>
    /// <returns>The list of contained modules.</returns>
    public IReadOnlyList<EntityUid> GetContainedModules(Entity<ModuleStorageComponent?> storage)
    {
        // I'm expecting this to get passed invalid entities during shutdown, it's fine.
        if (!Resolve(storage, ref storage.Comp, false))
            return [];
        
        return storage.Comp.ModuleContainer.ContainedEntities;
    }

    /// <summary>
    /// Returns (one of) the slots containing the passed module, if it is present in the provided storage.
    /// </summary>
    /// <param name="storage">The storage to look in.</param>
    /// <param name="module">The module to search for.</param>
    /// <param name="slot">The slot the module is contained in.</param>
    /// <returns>Whether the module was found in the storage.</returns>
    public bool TryGetModuleContainingSlot(Entity<ModuleStorageComponent?> storage, Entity<ModuleComponent?> module,
        [NotNullWhen(true)] out int? slot)
    {
        slot = null;
        if (!Resolve(storage, ref storage.Comp) || !Resolve(module, ref module.Comp))
            return false;

        foreach (var slotData in storage.Comp.ModuleSlots)
        {
            if (slotData.Value == module)
            {
                slot = slotData.Key;
                return true;
            }
        }

        return false;
    }
    
    #endregion
}

[Serializable, NetSerializable]
public enum ModuleUiKey : byte
{
    Key,
}