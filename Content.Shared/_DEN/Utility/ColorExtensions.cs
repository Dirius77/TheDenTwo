using System.Numerics;

namespace Content.Shared._DEN.Utility;

public static class ColorExtensions
{
    /// <summary>
    ///     Gets the euclidean distance beteween two colors.
    /// </summary>
    public static float GetColorDistance(this Color a, Color b)
    {
        var va = a.RGBA;
        var vb = b.RGBA;
        var diff = va - vb;
        return Vector4.Dot(diff, diff);
    }
}
