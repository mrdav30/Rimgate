using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class PawnsArrivalModeWorker_Gate : PawnsArrivalModeWorker
{
    public override bool CanUseOnMap(Map map)
    {
        return Building_Gate.TryGetSpawnedGateOnMap(map, out Building_Gate gate) && !gate.IsActive;
    }

    public override void Arrive(List<Pawn> pawns, IncidentParms parms)
    {
        Map map = (Map)parms.target;
        if (!Building_Gate.TryGetSpawnedGateOnMap(map, out Building_Gate gateOnMap)) return;

        gateOnMap.TicksSinceBufferUnloaded = -150;
        foreach (Pawn pawn in pawns)
            gateOnMap.AddToReceiveBuffer(pawn);
        gateOnMap.ForceLocalOpenAsReceiver();
    }

    public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
    {
        Map map = (Map)parms.target;
        if (!Building_Gate.TryGetSpawnedGateOnMap(map, out Building_Gate gateOnMap) || gateOnMap.IsActive)
        {
            parms.raidArrivalMode = map.Tile.LayerDef.isSpace
                ? PawnsArrivalModeDefOf.EdgeDrop
                : PawnsArrivalModeDefOf.EdgeWalkIn;
            return parms.raidArrivalMode?.Worker?.TryResolveRaidSpawnCenter(parms) ?? false;
        }

        parms.spawnRotation = gateOnMap.Rotation;
        parms.spawnCenter = gateOnMap.Position;
        return true;
    }
}
