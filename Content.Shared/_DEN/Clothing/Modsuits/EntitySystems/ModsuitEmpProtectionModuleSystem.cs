using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared._DEN.Modules.Components;
using Content.Shared._DEN.Modules.EntitySystems;
using Content.Shared._DEN.Wires.Components;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public sealed partial class ModsuitEmpProtectionModuleSystem : EntitySystem
{
    [Dependency] private SharedModuleStorageSystem _moduleStorage = default!;
    [Dependency] private SharedModsuitSystem _modsuitSystem = default!;
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModsuitEmpProtectionModuleComponent, ModsuitRelayedEvent<AttemptEmpBreakWiresEvent>>(OnAttemptEmpBreakWiresEvent);
    }

    private void OnAttemptEmpBreakWiresEvent(Entity<ModsuitEmpProtectionModuleComponent> ent, ref ModsuitRelayedEvent<AttemptEmpBreakWiresEvent> args)
    {

        if (!_modsuitSystem.TryGetModuleController(ent.Owner, out var controller))
            return;

        if (!TryComp<ModuleStorageComponent>(controller.Value.Owner, out var storageComp))
            return;
        
        var storage = controller.Value.Owner;
        
        if (!_moduleStorage.TryGetModuleContainingSlot(storage, ent.Owner, out var slot))
            return;
        
        _moduleStorage.TryRemoveModule((storage, storageComp), null, null, slot.Value, out var removedModule, true);
        if (removedModule != ent.Owner)
        {
            Log.Warning($"TryRemoveModule removed module {ToPrettyString(removedModule)} rather than the intended {ToPrettyString(ent.Owner)}");
        }
        
        _popupSystem.PopupPredicted(Loc.GetString("modsuit-emp-protection-module-breaks", ("entity", ent)), args.Owner, null, PopupType.LargeCaution);
        PredictedQueueDel(ent);
        args.Args.Cancel();

        // Client can't predict spawning.
        if (!_netManager.IsServer)
            return;
        
        var brokenModule = Spawn(ent.Comp.BrokenProtoId);
        _moduleStorage.TryInsertModule((storage, storageComp), brokenModule, null, slot.Value);
    }
}