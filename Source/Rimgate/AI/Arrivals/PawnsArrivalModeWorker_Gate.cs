using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class PawnsArrivalModeWorker_Gate : PawnsArrivalModeWorker
{
    public override bool CanUseOnMap(Map map)
    {
        Building_Gate gate = Building_Gate.GetGateOnMap(map);
        return gate != null && !gate.IsActive;
    }

    public override void Arrive(List<Pawn> pawns, IncidentParms parms)
    {
        Map map = (Map)parms.target;
        Building_Gate gateOnMap = Building_Gate.GetGateOnMap(map);
        if (gateOnMap == null) return;

        gateOnMap.TicksSinceBufferUnloaded = -150;
        foreach (Pawn pawn in pawns)
            gateOnMap.AddToReceiveBuffer(pawn);
        gateOnMap.ForceLocalOpenAsReceiver();
    }

    public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
    {
        Map map = (Map)parms.target;
        Building_Gate gateOnMap = Building_Gate.GetGateOnMap(map);
        if (gateOnMap == null || gateOnMap.IsActive == true)
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
