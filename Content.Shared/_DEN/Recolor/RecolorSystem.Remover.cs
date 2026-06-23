using Content.Shared._DEN.Recolor.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._DEN.Recolor;

public sealed partial class RecolorSystem
{
    private void OnRecolorRemoverAfterInteract(Entity<RecolorRemoverComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null
            || args.Handled
            || !args.CanReach
            || _whitelist.CheckBoth(args.Target, ent.Comp.EntityBlacklist, ent.Comp.EntityWhitelist))
            return;
        if (!TryComp<RecoloredComponent>(args.Target, out var recolored) || !recolored.RecolorData.Removable)
            return;

        args.Handled = TryStartRemoveRecolorDoAfter(args.User, (args.Target.Value, recolored), ent);
    }

    private bool TryStartRemoveRecolorDoAfter(
        EntityUid user,
        Entity<RecoloredComponent> target,
        Entity<RecolorRemoverComponent> remover)
    {
        var doAfterEvent = new RemoveRecolorDoAfterEvent();

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user: user,
            seconds: (float)remover.Comp.DoAfterDuration.TotalSeconds,
            @event: doAfterEvent,
            eventTarget: remover,
            target: target,
            used: remover)
        {
            BreakOnDropItem = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        var recolorData = target.Comp.RecolorData;

        if (recolorData.PaintType != null)
            _popup.PopupClient(Loc.GetString("recolor-remover-start-popup", ("name", target), ("paintType", recolorData.PaintType)), remover, user);

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnRemoveRecolorDoAfterEvent(Entity<RecolorRemoverComponent> ent, ref RemoveRecolorDoAfterEvent args)
    {
        if (args.Handled
            || args.Cancelled
            || !TryComp<RecoloredComponent>(args.Target, out var recolored)
            || _whitelist.CheckBoth(args.Target, ent.Comp.EntityBlacklist, ent.Comp.EntityWhitelist))
            return;

        var recolorData = recolored.RecolorData;

        RemoveRecolor((args.Target.Value, recolored));

        _audio.PlayPredicted(ent.Comp.DoafterSound, ent, args.User);

        if (recolorData.PaintType != null)
            _popup.PopupClient(Loc.GetString("recolor-remover-finish-popup", ("name", args.Target), ("paintType", recolorData.PaintType)), ent, args.User);

        args.Handled = true;
    }

    private void OnGetRemoverVerbs(Entity<RecolorRemoverComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<RecoloredComponent>(args.Target, out var recolored) || !recolored.RecolorData.Removable)
            return;

        var user = args.User;
        var target = args.Target;

        var verb = new UtilityVerb
        {
            Act = () => TryStartRemoveRecolorDoAfter(user, (target, recolored), ent),
            Text = Loc.GetString("verb-remove-recolor"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/bubbles.svg.192dpi.png")),
        };

        args.Verbs.Add(verb);
    }
}
