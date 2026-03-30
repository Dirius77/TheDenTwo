using Content.Client.Resources;
using Content.Client.UserInterface;
using Content.Shared._DEN.Clothing.Modsuits.Components;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;

namespace Content.Client._DEN.UserInterface.Systems.Modsuits.Controls;

public sealed class ModuleGridPiece : Control, IEntityControl
{
    private readonly IEntityManager _entityManager;

    public readonly EntityUid Entity;

    public event Action<EntityUid>? ModuleClicked;

    public ModuleGridPiece(Entity<ModsuitModuleComponent> entity, IEntityManager entityManager, IResourceCache resourceCache)
    {
        IoCManager.InjectDependencies(this);
        
        _entityManager = entityManager;

        Entity = entity.Owner;
        
        Visible = true;
        MouseFilter = MouseFilterMode.Stop;
        
        TooltipSupplier = SupplyTooltip;

        var texture = new TextureRect
        {
            Texture = resourceCache.GetTexture(entity.Comp.UITexture),
        };
        
        AddChild(texture);
    }

    private Control? SupplyTooltip(Control sender)
    {
        return new Tooltip
        {
            Text = _entityManager.GetComponent<MetaDataComponent>(Entity).EntityName
        };
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
        {
            return;
        }
        
        ModuleClicked?.Invoke(Entity);
    }
    
    public EntityUid? UiEntity => Entity;
}