using System.Linq;
using Content.Client._DEN.Clothing.Sealable.Components;
using Content.Shared._DEN.Clothing.Sealable.EntitySystems;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;

namespace Content.Client._DEN.Clothing.Sealable.EntitySystems;

public sealed partial class SealableClothingVisualizerSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SealableClothingVisualsComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals,
            after: [typeof(ClothingSystem)]);
    }

    private void OnGetEquipmentVisuals(Entity<SealableClothingVisualsComponent> entity, ref GetEquipmentVisualsEvent args)
    {
        if (!_appearanceSystem.TryGetData<bool>(entity, SealableClothingVisuals.State, out var state) || !state)
            return;
        
        if (!TryComp(args.Equipee, out InventoryComponent? inventory))
            return;

        List<PrototypeLayerData>? layers = null;
        
        // Try getting species variants first.
        if (inventory.SpeciesId != null)
            entity.Comp.ClothingVisuals.TryGetValue($"{args.Slot}-{inventory.SpeciesId}", out layers);
        
        // Fallback to default.
        if (layers == null && !entity.Comp.ClothingVisuals.TryGetValue(args.Slot, out layers))
            return;

        // Loop over all the defined layers and add them.
        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? $"{args.Slot}-sealed" : $"{args.Slot}-sealed-{i}";
                i++;
            }
            
            args.Layers.Add((key, layer));
        }
    }
}