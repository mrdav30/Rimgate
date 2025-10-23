using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class PlaceWorker_Stargate : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(
       BuildableDef checkingDef,
       IntVec3 loc,
       Rot4 rot,
       Map map,
       Thing thingToIgnore = null,
       Thing thing = null)
    {
        if (Building_Stargate.GetStargateOnMap(map, thing) != null)
            return new AcceptanceReport("RG_OnlyOneSGPerMap".Translate());

        // Pocket Maps do not have an associated PlanetTile, 
        // so no gate address, so stargates cannot work on them
        if (map.IsPocketMap)
            return new AcceptanceReport("RG_PocketMapDisallowed".Translate());

        return true;
    }

    public override void DrawGhost(
        ThingDef def,
        IntVec3 c,
        Rot4 rot,
        Color ghostCol,
        Thing thing = null)
    {
        base.DrawGhost(def, c, rot, ghostCol, thing);
        var compProps = def.GetCompProperties<CompProperties_StargateControl>();
        if (compProps == null) return;

        List<IntVec3> pattern = new List<IntVec3>();
        foreach (var off in RotatedPatternLocal(rot, compProps.vortexPattern))
        {
            var cell = c + off;
            pattern.Add(cell);
        }

        GenDraw.DrawFieldEdges(pattern, Color.red);
    }

    private static IEnumerable<IntVec3> RotatedPatternLocal(Rot4 rot, IEnumerable<IntVec3> offsets)
    {
        foreach (var off in offsets)
            yield return Utils.RotateOffset(off, rot);
    }
}
