using Content.Shared.Actions;

namespace Content.Shared._DEN.Actions.Components;

/// <summary>
/// Used on an entity to communicate that it should get InteractInWorldActionEvents
/// </summary>
[RegisterComponent]
public sealed partial class InteractInWorldActionComponent : Component;

public sealed partial class InteractInWorldActionEvent : WorldTargetActionEvent;