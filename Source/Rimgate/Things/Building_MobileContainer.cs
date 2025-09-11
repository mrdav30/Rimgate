using RimWorld;
using System.Collections;
using System.Collections.Generic;
using Verse;
using static HarmonyLib.Code;

namespace Rimgate;

public class Building_MobileContainer : Building
{
    public Comp_MobileContainer Mobile
    {
        get
        {
            _cachedMobile ??= GetComp<Comp_MobileContainer>();
            return _cachedMobile;
        }
    }

    private Comp_MobileContainer _cachedMobile;

    public ThingOwner TryGetInnerContainer() => Mobile?.InnerContainer;

    public override void DrawExtraSelectionOverlays()
    {
        if (!Spawned || Mobile == null) return;
        GenDraw.DrawRadiusRing(Position, Mobile.Props.loadRadius);
    }
}
