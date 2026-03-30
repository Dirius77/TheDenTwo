namespace Content.Shared._DEN.Clothing.Modsuits.Components;

[RegisterComponent]
public sealed partial class ModsuitModuleComponent : Component
{
    /// <summary>
    /// How much 'bus space' this module consumes when installed.
    /// </summary>
    [DataField(required: true)] public int BusWidth;

    [DataField(required: true)] public string UITexture;
}