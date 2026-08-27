using Content.Shared._DEN.Language.Components;

namespace Content.Shared.Radio.EntitySystems;

public abstract partial class SharedRadioDeviceSystem
{
    [Dependency] private EntityQuery<RadioTransmittableComponent> _radioLang = default!;
}
