using Content.Shared._DEN.Clothing.Modsuits.Components;
using Content.Shared.Interaction;
using Content.Shared.Wires;
using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Clothing.Modsuits.EntitySystems;

public abstract partial class SharedModsuitSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<ModsuitControlComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnActivateInWorld(Entity<ModsuitControlComponent> entity, ref ActivateInWorldEvent args)
    {
        Log.Debug("Ough~");
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
}

[Serializable, NetSerializable]
public enum ModsuitModuleUiKey : byte
{
    Key,
}