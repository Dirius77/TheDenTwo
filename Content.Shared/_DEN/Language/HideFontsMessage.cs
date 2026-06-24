using Content.Shared._DEN.CCVar;
using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Language;

[Serializable, NetSerializable]
public sealed class HideFontsMessage : EntityEventArgs
{
    public HideLanguageFontSetting Hide { get; }

    public HideFontsMessage(HideLanguageFontSetting hide)
    {
        Hide = hide;
    }
}
