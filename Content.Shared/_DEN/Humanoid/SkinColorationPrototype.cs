using Content.Shared._DEN.Utility;

namespace Content.Shared.Humanoid;

public sealed partial class SkinColorationPrototype
{
    /// <summary>
    ///     An optional label for the "alt strategy" toggle button in the UI.
    /// </summary>
    [DataField]
    public LocId? ToggleButtonText = null;

    /// <summary>
    ///     Gets the closest verified color, taking all strategies into account.
    /// </summary>
    /// <param name="color">The color to verify.</param>
    /// <returns>A verified color.</returns>
    public Color VerifyColor(Color color, out bool altIsCloser)
    {
        altIsCloser = false;
        var primary = Strategy.EnsureVerified(color);

        if (AltStrategy == null)
            return primary;

        var alt = AltStrategy.EnsureVerified(color);

        // Pick the closer color.
        var pD = color.GetColorDistance(primary);
        var aD = color.GetColorDistance(alt);
        altIsCloser = aD < pD;

        return altIsCloser ? alt : primary;
    }
}

public sealed partial class HumanTonedSkinColoration
{
    public Color ClosestSkinColor(Color color)
    {
        // More accurate "closest value"
        return FromUnary(ToUnary(color));
    }
}
