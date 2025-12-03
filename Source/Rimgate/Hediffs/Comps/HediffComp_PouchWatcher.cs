using UnityEngine;
using Verse;

namespace Rimgate;

public class HediffComp_PouchWatcher : HediffComp
{
    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();

        Pawn pawn = parent.pawn;
        if (pawn == null || pawn.health == null || pawn.Map == null)
            return;

        // If the pawn has a prim'ta, remove it and spawn the item
        var primta = pawn.GetHediffOf(RimgateDefOf.Rimgate_PrimtaInPouch);
        if (primta == null)
            return;

        pawn.health.RemoveHediff(primta);

        var thing = ThingMaker.MakeThing(RimgateDefOf.Rimgate_PrimtaSymbiote);
        GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
    }
}
