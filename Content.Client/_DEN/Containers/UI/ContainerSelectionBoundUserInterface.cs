using Content.Client._DEN.Containers.EntitySystems;
using Content.Shared._DEN.Containers.Components;
using Content.Shared.EntityTable;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._DEN.Containers.UI;

[UsedImplicitly]
public sealed class ContainerSelectionBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private ContainerSelectionWindow? _window;

    public ContainerSelectionBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<EntityTableContainerSelectionComponent>(Owner, out var entityTableSelectComp))
            return;

        _window = this.CreateWindow<ContainerSelectionWindow>();
        var containers = ConvertToContainers(entityTableSelectComp);
        _window.SetWindows(containers);
        _window.OpenCentered();

    }

    private IEnumerable<BoxContainer> ConvertToContainers(EntityTableContainerSelectionComponent entityTableSelectComp)
    {
        var containers = new BoxContainer[entityTableSelectComp.Selections.Count];
        var containerIndex = 0;

        foreach (var selection in entityTableSelectComp.Selections)
        {
            var container = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
            };

            var innerContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
            };

            var wrapper = new PanelContainer
            {
                VerticalExpand = false,
                HorizontalExpand = false,
                Margin = new Thickness(4),
                PanelOverride = new StyleBoxFlat{BorderThickness = new Thickness(2), BorderColor =  Color.FromHex("#2F2F2F")},
            };

            var header = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                Margin = new Thickness(2),
            };

            var label = new Label
            {
                Text = Loc.GetString(selection.SelectionName),
                HorizontalExpand = true,
                Margin = new Thickness(4),
            };

            var button = new Button { Text = Loc.GetString("container-selection-ui-choose-button") };
            var index = containerIndex;
            button.OnPressed += _ => MakeSelection(index);

            header.AddChild(label);
            header.AddChild(button);
            innerContainer.AddChild(header);

            var cbody = new CollapsibleBody
            {
                HorizontalExpand = true,
                Margin = new Thickness(6f),
            };

            var body = new GridContainer()
            {
                VerticalExpand = false,
                HorizontalExpand = false,
                MaxGridWidth = 300f,
            };

            foreach (var entContainer in selection.Containers)
            {
                var ctx = new EntityTableContext();
                foreach (var (proto, _) in entContainer.Value.ListSpawns(EntMan, _prototypeManager, ctx))
                {
                    var entProtoView = new EntityPrototypeView
                    {
                        SetSize = new (32f),
                        Stretch = SpriteView.StretchMode.Fill,
                        Scale = new(2),
                    };
                    entProtoView.SetPrototype(proto);

                    var viewPanel = new PanelContainer
                    {
                        VerticalExpand = false,
                        HorizontalExpand = false,
                        Margin = new Thickness(4),
                        PanelOverride = new StyleBoxFlat{BorderColor = Color.FromHex("#4f4f4f"), BorderThickness = new Thickness(2)},
                    };

                    viewPanel.AddChild(entProtoView);

                    body.AddChild(viewPanel);
                }
            }

            cbody.AddChild(body);

            var collapsible = new Collapsible(Loc.GetString("container-selection-ui-contents"), cbody)
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
                Margin = new Thickness(4),
            };
            innerContainer.AddChild(collapsible);
            wrapper.AddChild(innerContainer);
            container.AddChild(wrapper);

            containers[containerIndex++] = container;
        }

        return containers;
    }

    private void MakeSelection(int index)
    {
        var containerSelection = EntMan.System<ContainerSelectionSystem>();

        containerSelection.SendSelectionEvent(Owner, index);
        _window?.Close();
    }
}
