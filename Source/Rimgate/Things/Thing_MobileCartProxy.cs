using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Thing_MobileCartProxy : ThingWithComps
{
    public override Graphic Graphic => RimgateTex.EmptyGraphic;

    public Comp_MobileContainerControl Control => _cachedMobile ??= GetComp<Comp_MobileContainerControl>();

    private Comp_MobileContainerControl _cachedMobile;

    // proxy -> Docked at a specific cell, using the original cart def when possible
    public Building_MobileContainer ConvertProxyToCart(
        Pawn p,
        ThingDef cartDef,
        IntVec3? at = null,
        Rot4? rot = null,
        bool spawn = true)
    {
        if (Control == null) return null;

        // Make cart with original stuff (if any), then spawn that instance
        var made = ThingMaker.MakeThing(cartDef, Control.SavedStuff) as Building_MobileContainer;
        made.HitPoints = Mathf.Clamp(Control.SavedHitPoints, 1, made.MaxHitPoints);

        if (spawn)
        {
            var map = p.Map;
            if (at == null || !at.Value.InBounds(map)) at = p.Position;
            if (rot == null) rot = p.Rotation;
            GenSpawn.Spawn(made, at.Value, map, rot.Value);
        }

        var ccomp = made.Control;

        // Move back contents
        Control.InnerContainer.TryTransferAllToContainer(ccomp.InnerContainer);

        // restore fuel (if any)
        if (ccomp.Refuelable != null)
        {
            // CompRefuelable starts at 0 on fresh Thing; add whatever is left from pushing
            if (Control != null && Control.PushingFuel > 0f)
                ccomp.Refuelable.Refuel(Control.PushingFuel);
        }

        var paint = made.TryGetComp<CompColorable>();
        if (paint != null && Control.SavedHasPaint)
            paint.SetColor(Control.SavedDrawColor);

        Control.Detach();
        Destroy(DestroyMode.Vanish);

        return made;
    }
}

