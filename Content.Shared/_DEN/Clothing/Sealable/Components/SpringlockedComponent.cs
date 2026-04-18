using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DEN.Clothing.Sealable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpringlockedComponent : Component
{
    [DataField, AutoNetworkedField] public ProtoId<ToolQualityPrototype> OpenTool = "Prying";

    [DataField, AutoNetworkedField] public float OpenTimeMultiplier = 2.5f;

    [DataField, AutoNetworkedField] public bool HasBeenForced = false;
    
    [DataField] public LocId FailedUnsealMessage = "springlock-could-not-open";
    
    [DataField] public LocId FailedSealMessage = "springlock-could-not-close";
    
    [DataField, AutoNetworkedField] 
    public SoundSpecifier? SoundForceOpen = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };
}