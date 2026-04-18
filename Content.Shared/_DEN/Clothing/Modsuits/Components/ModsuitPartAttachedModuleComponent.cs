using Robust.Shared.GameStates;

namespace Content.Shared._DEN.Clothing.Modsuits.Components;

/// <summary>
/// Marks that a modsuit module's functionality is attached to specific parts of the modsuit, indicated by the slots
/// that those pieces go in. This current relies on the controller having a ToggleableClothingComponent that the
/// matching parts are fetched from.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModsuitPartAttachedModuleComponent : Component
{
    /// <summary>
    /// The slots that this module is considered to be 'attached' to for the sake of modsuit functionality.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<string> Slots;

    /// <summary>
    /// If all the listed slots must be present and sealed, or just one.
    /// </summary>
    [DataField, AutoNetworkedField] public bool NeedsAll = true;
}