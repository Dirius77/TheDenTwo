using Content.Shared._DEN.Language.EntitySystems;

namespace Content.Shared._DEN.Language.Components;

[RegisterComponent]
[Access(typeof(SharedSyllableScramblingSystem))]
public sealed partial class SyllableScramblingComponent : Component
{
    [DataField]
    public int MinSyllables { get; private set; } = 1;

    [DataField]
    public int MaxSyllables { get; private set; } = 3;

    [DataField(required: true)]
    public List<string> Syllables { get; private set; } = new();
}
