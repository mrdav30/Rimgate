using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using static UnityEngine.AudioSettings;

namespace Rimgate;

public class PlaceWorker_ResonanceIdol : PlaceWorker
{
    public override void DrawGhost(
        ThingDef def,
        IntVec3 center,
        Rot4 rot,
        Color ghostCol,
        Thing thing = null)
    {
        base.DrawGhost(def, center, rot, ghostCol, thing);
        foreach (CompProperties props in def.comps)
        {
            if (props is not CompProperties_ResonanceIdol riProps)
                continue;

            GenDraw.DrawRadiusRing(center, riProps.radius);
            return;
        }
    }
}
