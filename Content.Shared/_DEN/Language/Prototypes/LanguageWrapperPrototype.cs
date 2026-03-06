using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Language;

[Prototype]
public sealed partial class LanguageWrapperPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Dialog;

    [DataField(required: true)]
    public LocId Emote;

    [DataField(required: true)]
    public LocId Language;

    [DataField(required: true)]
    public LocId Prefix;

    [DataField(required: true)]
    public LocId Message;

    [DataField(required: true)]
    public LocId SingularMessage;

    [DataField(required: true)]
    public LocId BoldType;
}
