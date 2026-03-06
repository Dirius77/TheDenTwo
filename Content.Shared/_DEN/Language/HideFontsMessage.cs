using Content.Shared._DEN.CCVars;
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
