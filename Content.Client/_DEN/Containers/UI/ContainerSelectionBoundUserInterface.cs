using Content.Client._DEN.Containers.EntitySystems;
using Content.Shared._DEN.Containers.Components;
using Content.Shared._DEN.Containers.Events;
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
        var controls = ConvertToControls(entityTableSelectComp);
        _window.SetControls(controls);
        _window.OpenCentered();

    }

    private IEnumerable<ContainerSelectionControl> ConvertToControls(EntityTableContainerSelectionComponent entityTableSelectComp)
    {
        var containers = new ContainerSelectionControl[entityTableSelectComp.Selections.Count];
        var containerIndex = 0;

        // Create a ContainerSelectionControl for each of the possible selections.
        foreach (var selection in entityTableSelectComp.Selections)
        {
            var selectionControl = new ContainerSelectionControl();
            selectionControl.SetData(EntMan, selection.SelectionName, selection.Containers);
            var curIndex = containerIndex;
            selectionControl.ChooseButton.OnPressed += _ => MakeSelection(curIndex);

            containers[containerIndex++] = selectionControl;
        }

        return containers;
    }

    private void MakeSelection(int index)
    {
        SendMessage(new ContainerSelectionMessage(index));
        _window?.Close();
    }
}
