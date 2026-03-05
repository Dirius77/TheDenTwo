using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Language;

[Serializable, NetSerializable]
public sealed class HideFontsMessage : EntityEventArgs
{
    public bool Hide { get; }

    public HideFontsMessage(bool hide)
    {
        Hide = hide;
    }
}
