using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class PlaceWorker_Gate : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(
       BuildableDef checkingDef,
       IntVec3 loc,
       Rot4 rot,
       Map map,
       Thing thingToIgnore = null,
       Thing thing = null)
    {
        if (Building_Gate.TryGetSpawnedGateOnMap(map, out _, thingToIgnore: thing))
            return new AcceptanceReport("RG_CannotPlace_Gate".Translate(checkingDef.LabelCap, "RG_OnlyOneSGPerMap".Translate()));

        if(GateUtil.AddressBookFull)
            return new AcceptanceReport("RG_CannotPlace_Gate".Translate(checkingDef.LabelCap, "RG_Cannot_AddressBookFull".Translate()));

        // Pocket Maps do not have an associated PlanetTile, 
        // which is required for gate functionality.
        if (map.IsPocketMap)
            return new AcceptanceReport("RG_CannotPlace_Gate".Translate(checkingDef.LabelCap, "RG_PocketMapDisallowed".Translate()));

        return true;
    }

    public override void DrawGhost(
        ThingDef def,
        IntVec3 c,
        Rot4 rot,
        Color ghostCol,
        Thing thing = null)
    {
        var props = def.GetModExtension<Building_Gate_Ext>();
        if (props == null) return;

        List<IntVec3> pattern = new List<IntVec3>();
        foreach (var off in RotatedPatternLocal(rot, props.vortexPattern))
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
