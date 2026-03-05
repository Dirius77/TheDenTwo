namespace Content.Shared._DEN.Language.Components;

/// <summary>
///     Marks a language as being audible. Things that listen for speech such as triggers and parrots care about this.
/// </summary>
[RegisterComponent]
public sealed partial class AudibleComponent : Component;
