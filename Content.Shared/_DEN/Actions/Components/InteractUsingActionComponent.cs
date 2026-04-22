using Content.Shared.Actions;

namespace Content.Shared._DEN.Actions.Components;

/// <summary>
/// Marks an entity as usable with InteractUsingActionEvents
/// </summary>
[RegisterComponent]
public sealed partial class InteractUsingActionComponent : Component;

public sealed partial class InteractUsingActionEvent : EntityTargetActionEvent;