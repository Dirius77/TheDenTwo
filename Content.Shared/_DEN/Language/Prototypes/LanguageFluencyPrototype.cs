using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language;

[Prototype]
public sealed partial class LanguageFluencyPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = default!;

    [DataField(required: true)]
    public int Understanding;

    // Controls if this fluency is included as an option during character creation.
    [DataField] public bool RoundStart = true;

    public Color Color => Color.InterpolateBetween(Color.Green, Color.Red, (float)(Understanding / 100.0));

    public int CompareTo(LanguageFluencyPrototype? other)
    {
        return other is null ? 0 : Understanding.CompareTo(other.Understanding);
    }

    public static bool operator <(LanguageFluencyPrototype lhs, LanguageFluencyPrototype rhs)
    {
        return lhs.Understanding < rhs.Understanding;
    }

    public static bool operator >(LanguageFluencyPrototype lhs, LanguageFluencyPrototype rhs)
    {
        return lhs.Understanding > rhs.Understanding;
    }

    public static bool operator <=(LanguageFluencyPrototype lhs, LanguageFluencyPrototype rhs)
    {
        return lhs.Understanding <= rhs.Understanding;
    }

    public static bool operator >=(LanguageFluencyPrototype lhs, LanguageFluencyPrototype rhs)
    {
        return lhs.Understanding >= rhs.Understanding;
    }
}
