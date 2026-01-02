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
    public override void Arrive(List<Pawn> pawns, IncidentParms parms)
    {
        Map map = (Map)parms.target;
        Building_Stargate stargateOnMap = Building_Stargate.GetStargateOnMap(map);
        Comp_StargateControl sgComp = stargateOnMap?.GateControl;
        if (sgComp == null) return;

        sgComp.TicksSinceBufferUnloaded = -150;
        foreach (Pawn pawn in pawns)
            sgComp.AddToReceiveBuffer(pawn);
        sgComp.ForceLocalOpenAsReceiver();
    }

    public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
    {
        Map map = (Map)parms.target;
        Building_Stargate stargateOnMap = Building_Stargate.GetStargateOnMap(map);
        Comp_StargateControl sgComp = stargateOnMap?.GateControl;
        if (sgComp == null || sgComp.IsActive == true)
        {
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            return parms.raidArrivalMode?.Worker?.TryResolveRaidSpawnCenter(parms) ?? false;
        }

        parms.spawnRotation = stargateOnMap.Rotation;
        parms.spawnCenter = stargateOnMap.Position;
        return true;
    }
}
