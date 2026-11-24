using UnityEngine;
using Verse;

namespace Rimgate;

public class PlaceWorker_MobileContainer : PlaceWorker
{
    public override void DrawGhost(
        ThingDef def,
        IntVec3 c,
        Rot4 rot,
        Color ghostCol,
        Thing thing = null)
    {
        var ext = def.GetModExtension<Building_MobileContainer_Ext>();
        if (ext == null) return;

        GenDraw.DrawRadiusRing(c, ext.loadRadius);
    }
}
