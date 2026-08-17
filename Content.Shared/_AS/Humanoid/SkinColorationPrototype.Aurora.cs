namespace Content.Shared.Humanoid;

public sealed partial class SkinColorationPrototype
{
    /// <summary>
    ///     An "alternate strategy" for selecting a character's skin color.
    ///     If this is specified, players have the option to toggle between
    ///     the primary and alternate skin color strategies in the character editor.
    ///     This allows a species to have two possible color schemes.
    /// </summary>
    [DataField]
    public ISkinColorationStrategy? AltStrategy;
}
