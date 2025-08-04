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
        sb.Append("Rimgate_GateAddress".Translate(Comp_Stargate.GetStargateDesignation(site.Tile)));
        return sb.ToString();
    }

    public override void PostMapGenerate(Map map)
    {
        base.PostMapGenerate(map);
        if (map == null)
        {
            Log.Error("Rimgate :: SitePartWorker map was null on PostMapGenerate. That makes no sense.");
            return;
        }

        Thing gateOnMap = Comp_Stargate.GetStargateOnMap(map);
        var VortexCells = gateOnMap.TryGetComp<Comp_Stargate>().VortexCells;

        //move pawns away from vortex
        foreach (Pawn pawn in map.mapPawns.AllPawns)
        {
            Room pawnRoom = pawn.Position.GetRoom(pawn.Map);
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
