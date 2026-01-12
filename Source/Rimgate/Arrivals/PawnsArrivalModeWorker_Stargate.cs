using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class PawnsArrivalModeWorker_Stargate : PawnsArrivalModeWorker
{
    public override bool CanUseOnMap(Map map)
    {
        Building_Stargate stargate = Building_Stargate.GetStargateOnMap(map);
        return stargate != null && !stargate.IsActive;
    }

    public override void Arrive(List<Pawn> pawns, IncidentParms parms)
    {
        Map map = (Map)parms.target;
        Building_Stargate stargateOnMap = Building_Stargate.GetStargateOnMap(map);
        if (stargateOnMap == null) return;

        stargateOnMap.TicksSinceBufferUnloaded = -150;
        foreach (Pawn pawn in pawns)
            stargateOnMap.AddToReceiveBuffer(pawn);
        stargateOnMap.ForceLocalOpenAsReceiver();
    }

    public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
    {
        Map map = (Map)parms.target;
        Building_Stargate stargateOnMap = Building_Stargate.GetStargateOnMap(map);
        if (stargateOnMap == null || stargateOnMap.IsActive == true)
        {
            parms.raidArrivalMode = map.Tile.LayerDef.isSpace
                ? PawnsArrivalModeDefOf.EdgeDrop
                : PawnsArrivalModeDefOf.EdgeWalkIn;
            return parms.raidArrivalMode?.Worker?.TryResolveRaidSpawnCenter(parms) ?? false;
        }

        parms.spawnRotation = stargateOnMap.Rotation;
        parms.spawnCenter = stargateOnMap.Position;
        return true;
    }
}
