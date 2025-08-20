using System;
using System.Linq;
using Verse;
using RimWorld.Planet;
using RimWorld;
using System.Text;

namespace Rimgate;

public class SitePartWorker_Stargate : SitePartWorker
{
    public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Rimgate_GateAddress".Translate(StargateUtility.GetStargateDesignation(site.Tile)));
        return sb.ToString();
    }

    public override void PostMapGenerate(Map map)
    {
        if (map == null)
        {
            Log.Error("Rimgate :: SitePartWorker map was null on PostMapGenerate.");
            return;
        }

        Thing gateOnMap = StargateUtility.GetStargateOnMap(map);
        if (gateOnMap == null)
        {
            Log.Error("Rimgate :: SitePartWorker gateOnMap was null on PostMapGenerate.");
            return;
        }

        // move pawns away from vortex
        var VortexCells = gateOnMap.TryGetComp<Comp_Stargate>().VortexCells;
        foreach (Pawn pawn in map.mapPawns?.AllPawns)
        {
            if (pawn.Map == null) continue;

            Room pawnRoom = pawn.Position.GetRoom(pawn.Map);
            if (pawnRoom == null) continue;

            var cells = GenRadial.RadialCellsAround(pawn.Position, 9, true)
                .Where(c => c.InBounds(map)
                    && c.Walkable(map)
                    && c.GetRoom(map) == pawnRoom
                    && !VortexCells.Contains(c));
            if (!cells.Any())
                continue;

            pawn.Position = cells.RandomElement();
            pawn.pather.StopDead();
            pawn.jobs.StopAll();
        }
    }
}
