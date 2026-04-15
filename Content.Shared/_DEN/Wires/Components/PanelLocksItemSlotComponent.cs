namespace Content.Shared._DEN.Wires.Components;

[RegisterComponent]
public sealed partial class PanelLocksItemSlotComponent : Component
{
    [DataField(required: true)] public List<string> LockedSlots;
}