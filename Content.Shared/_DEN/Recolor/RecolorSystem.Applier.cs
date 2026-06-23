using Content.Shared._DEN.Recolor.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using JetBrains.Annotations;

namespace Content.Shared._DEN.Recolor;

public sealed partial class RecolorSystem
{
    /// <summary>
    /// Change the color any given recolor applier applies.
    /// </summary>
    /// <param name="ent">Recolor applier to change the color of.</param>
    /// <param name="color">Color to change to.</param>
    /// <param name="colorName">Name of the color you're changing to, purely for flavor.</param>
    [PublicAPI]
    public void ChangeColor(Entity<RecolorApplierComponent> ent, Color color, string? colorName = null)
    {
        var recolorData = ent.Comp.RecolorData;

        if (color == recolorData.Color)
            return;

        recolorData.Color = color;
        recolorData.ColorName = colorName ?? null;

        ent.Comp.RecolorData = recolorData;

        Dirty(ent);
    }

    private static void OnComponentStartup(Entity<RecolorApplierComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.MaxUses != null)
            ent.Comp.UsesLeft = ent.Comp.MaxUses.Value;
    }

    private void OnRecolorApplierAfterInteract(Entity<RecolorApplierComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach
            || args.Target == null
            || args.Handled
            || !CanRecolor(ent, args.User, args.Target.Value))
        {
            return;
        }

        args.Handled = TryStartApplyRecolorDoAfter(args.User, args.Target.Value, ent);
    }

    private bool TryStartApplyRecolorDoAfter(
        EntityUid user,
        EntityUid target,
        Entity<RecolorApplierComponent> applier)
    {
        var doAfterEvent = new ApplyRecolorDoAfterEvent
        {
            RecolorData = applier.Comp.RecolorData,
        };

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user: user,
            seconds: (float)applier.Comp.DoAfterDuration.TotalSeconds,
            @event: doAfterEvent,
            eventTarget: applier,
            target: target,
            used: applier)
        {
            BreakOnDropItem = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnApplyRecolorDoAfterEvent(Entity<RecolorApplierComponent> ent, ref ApplyRecolorDoAfterEvent args)
    {
        if (args.Target is null || args.Handled || args.Cancelled)
            return;

        Recolor(
            uid: args.Target.Value,
            recolorData: args.RecolorData,
            recolorer: ent
        );

        _audio.PlayPredicted(ent.Comp.DoAfterSound, ent, args.User);

        ent.Comp.UsesLeft -= 1;

        Dirty(ent);

        args.Handled = true;
    }

    private bool CanRecolor(Entity<RecolorApplierComponent> applier, EntityUid user, EntityUid target, bool? doPopup = true)
    {
        // Popup target is set here.
        EntityUid? popupTarget = doPopup == true ? user : null;

        if (_openable.IsClosed(applier, popupTarget, predicted: true))
            return false;

        // Check whitelist and blacklist
        if (!_whitelist.CheckBoth(target, applier.Comp.EntityBlacklist, applier.Comp.EntityWhitelist))
        {
            if (doPopup == true)
                _popup.PopupClient(Loc.GetString(applier.Comp.CantRecolorPopup, ("target", target)), applier, popupTarget);
            return false;
        }

        // Check if there's enough uses left
        if (applier.Comp is { UsesLeft: <= 0, MaxUses: not null })
        {
            if (doPopup == true)
                _popup.PopupClient(Loc.GetString(applier.Comp.NoMoreUsesPopup, ("name", applier)), applier, popupTarget);
            return false;
        }

        return true;
    }

    private void OnGetApplierVerbs(Entity<RecolorApplierComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || !CanRecolor(ent, args.User, args.Target, false))
            return;

        var user = args.User;
        var target = args.Target;

        var verb = new UtilityVerb
        {
            Act = () => TryStartApplyRecolorDoAfter(user, target, ent),
            Text = Loc.GetString(ent.Comp.VerbText),
            Icon = ent.Comp.VerbIcon,
        };

        args.Verbs.Add(verb);
    }

    private void OnExamined(Entity<RecolorApplierComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var recolorData = ent.Comp.RecolorData;

        var colorName = GetColorName(recolorData);

        if (ent.Comp.ColorShowcaseExamine != "")
            args.PushMarkup(Loc.GetString(ent.Comp.ColorShowcaseExamine, ("color", recolorData.Color.WithAlpha(255)), ("colorName", colorName)));

        // If max uses isn't null (signifying this item has infinite uses), show uses count
        if (ent.Comp.MaxUses != null && ent.Comp.UsesExamine != "")
            args.PushMarkup(Loc.GetString(ent.Comp.UsesExamine, ("uses", ent.Comp.UsesLeft)));
    }
}
