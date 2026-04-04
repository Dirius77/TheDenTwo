using System.Numerics;
using Content.Client.Resources;
using Content.Client.UserInterface;
using Content.Shared._DEN.Modules.Components;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;

namespace Content.Client._DEN.Modules.UI.Controls;

public sealed class ModuleGridPiece : Control, IEntityControl
{
    private readonly IEntityManager _entityManager;

    private readonly Entity<ModuleComponent> _entity;

    private TextureRect? _texture;
    
    public event Action<EntityUid>? OnModuleClicked;

    public ModuleGridPiece(Entity<ModuleComponent> entity, IEntityManager entityManager, IResourceCache resourceCache)
    {
        IoCManager.InjectDependencies(this);
        
        _entityManager = entityManager;

        _entity = entity;
        
        Visible = true;
        MouseFilter = MouseFilterMode.Stop;
        
        TooltipSupplier = SupplyTooltip;

        _texture = new TextureRect
        {
            Texture = resourceCache.GetTexture(entity.Comp.UITexture),
            TextureScale = new Vector2(2, 2),
            CanShrink = true,
        };
        
        AddChild(_texture);
    }

    private Control? SupplyTooltip(Control sender)
    {
        return new Tooltip
        {
            Text = _entityManager.GetComponent<MetaDataComponent>(_entity).EntityName
        };
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
        {
            return;
        }
        
        OnModuleClicked?.Invoke(_entity);
    }

    protected override bool HasPoint(Vector2 point)
    {
        if (_texture != null)
        {
            var size = _texture.Texture!.Size * 2 * UIScale;
            return point.X >= 0 && point.X <= size.X && point.Y >= 0 && point.Y <= size.Y;
        }

        return false;
    }
    
    public EntityUid? UiEntity => _entity;
}