using Content.Shared._DEN.Clothing.Sealable.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._DEN.Clothing.Sealable.EntitySystems;

public sealed partial class SpringlockSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SealableClothingSystem _sealableSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpringlockedComponent, ChangeSealStateAttemptEvent>(OnSealStateAttempt);
        SubscribeLocalEvent<SpringlockedComponent, InteractUsingEvent>(OnSpringlockInteract);
    }

    private void OnSealStateAttempt(Entity<SpringlockedComponent> entity, ref ChangeSealStateAttemptEvent args)
    {
        if (!entity.Comp.HasBeenForced)
        {
            _popupSystem.PopupPredicted(Loc.GetString(entity.Comp.FailedMessage, ("name", entity)), args.User, args.User, PopupType.MediumCaution);
            args.Cancel();
        }
        
        entity.Comp.HasBeenForced = false;
    }

    private void OnSpringlockInteract(Entity<SpringlockedComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled || !_sealableSystem.IsSealed(entity.Owner))
            return;

        if (TryComp<ToolComponent>(args.Used, out var toolComp) && _toolSystem.HasQuality(args.Used, entity.Comp.OpenTool, toolComp))
        {
            args.Handled = true;
            entity.Comp.HasBeenForced = true;
            _audioSystem.PlayPredicted(entity.Comp.SoundForceOpen, entity, args.User);
            _sealableSystem.TryToggleSeal(args.User, entity.Owner, timeMultiplier: entity.Comp.OpenTimeMultiplier, customSealMsg: "springlock-prying-seal", customUnsealMsg: "springlock-prying-unseal");
        }
    }
}