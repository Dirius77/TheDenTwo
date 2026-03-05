namespace Content.Shared._DEN.Language.Components;

[RegisterComponent]
public sealed partial class WhisperMuffleComponent : Component
{
    /// <summary>
    ///     Whether to muffle or simply completely hide the message.
    /// </summary>
    [DataField]
    public bool Muffle;

    [DataField]
    public float MuffleAmount = 0.2f;
}
