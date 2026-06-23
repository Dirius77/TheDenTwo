using System.Linq;
using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared._DEN.Recolor;
using Content.Shared._DEN.Recolor.Components;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Robust.Client.GameObjects;

namespace Content.Client._DEN.Recolor;

public sealed partial class RecolorVisualizerSystem : VisualizerSystem<RecoloredComponent>
{
    [Dependency] private ItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecoloredComponent, OnRecolorRemovedEvent>(OnRecolorRemoved);

        SubscribeLocalEvent<RecoloredComponent, GetInhandVisualsEvent>(ApplyRecolorInHands,
            after: [typeof(ItemSystem)]); // Done so ItemSystem can handle sprite layers first, otherwise we apply our changes to nothing.

        SubscribeLocalEvent<RecoloredComponent, GetEquipmentVisualsEvent>(ApplyRecolorEquipment,
            after: [typeof(ClientClothingSystem)]); // Same as above.
    }

    protected override void OnAppearanceChange(EntityUid uid, RecoloredComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite == null)
            return;

        ApplyRecolorSprite((uid, component), args.Sprite);
        _item.VisualsChanged(uid);
    }

    private void OnRecolorRemoved(Entity<RecoloredComponent> ent, ref OnRecolorRemovedEvent args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        RemoveRecolor(ent, sprite);
        _item.VisualsChanged(ent);
    }

    private void ApplyRecolorInHands(Entity<RecoloredComponent> ent, ref GetInhandVisualsEvent args)
    {
        ApplyRecolorLayers(ent, args.Layers);
    }

    private void ApplyRecolorEquipment(Entity<RecoloredComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        ApplyRecolorLayers(ent, args.Layers);
    }

    private void ApplyRecolorLayers(Entity<RecoloredComponent> ent, List<(string, PrototypeLayerData)> layers)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (!AppearanceSystem.TryGetData(ent, RecolorVisuals.RecolorData, out RecolorData recolorData, appearance))
            return;

        foreach (var (_, layerData) in layers)
        {
            // Apply Color
            layerData.Color = recolorData.Color;

            //Test shader whitelists and blacklists
            if (!AllowedShader(layerData.Shader, recolorData))
                continue;

            // Apply shaders
            layerData.Shader = recolorData.Shader;
        }
    }

    private void ApplyRecolorSprite(Entity<RecoloredComponent> ent, SpriteComponent sprite)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (!AppearanceSystem.TryGetData(ent, RecolorVisuals.RecolorData, out RecolorData recolorData, appearance))
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            if (!SpriteSystem.TryGetLayer((ent, sprite), i, out var layer, false))
                continue;

            // Apply color
            SpriteSystem.LayerSetColor(layer, recolorData.Color);

            var layerShader = layer.ShaderPrototype;

            if (!AllowedShader(layerShader?.Id, recolorData))
                continue;

            // Apply shaders
            if (recolorData.Shader != null)
                sprite.LayerSetShader(i, recolorData.Shader);
        }
    }

    private void RemoveRecolor(Entity<RecoloredComponent> ent, SpriteComponent sprite)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (!AppearanceSystem.TryGetData(ent, RecolorVisuals.RecolorData, out RecolorData recolorData, appearance))
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            // TODO: Make it possible to get the previous color and shaders, currently impossible due to sprite system being fully clientside

            if (!SpriteSystem.TryGetLayer((ent, sprite), i, out var layer, false))
                continue;

            // Remove colors
            SpriteSystem.LayerSetColor(layer, Color.White);

            // Remove shaders
            var layerShader = layer.ShaderPrototype;

            if (!AllowedShader(layerShader?.Id, recolorData))
                continue;

            sprite.LayerSetShader(i, null, null);
        }
    }

    private static bool AllowedShader(string? shader, RecolorData appearanceData)
    {
        if (shader == null)
            return true;

        return (appearanceData.ShaderBlacklist == null || !appearanceData.ShaderBlacklist.Contains(shader))
               && (appearanceData.ShaderWhitelist == null || appearanceData.ShaderWhitelist.Contains(shader));
    }
}
