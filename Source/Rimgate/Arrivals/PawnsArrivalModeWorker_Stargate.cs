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
        Building_Stargate stargateOnMap = StargateUtility.GetStargateOnMap(map);
        Comp_StargateControl sgComp = stargateOnMap?.StargateControl;
        if (sgComp == null) return;

        sgComp.OpenStargateDelayed(-1, 450);
        sgComp.TicksSinceBufferUnloaded = -150;
        sgComp.IsReceivingGate = true;
        foreach (Pawn pawn in pawns)
            sgComp.AddToReceiveBuffer(pawn);
    }

    public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
    {
        Map map = (Map)parms.target;
        parms.spawnRotation = Rot4.South;
        Building_Stargate stargateOnMap = StargateUtility.GetStargateOnMap(map);
        Comp_StargateControl sgComp = stargateOnMap?.StargateControl;

        if (sgComp == null || sgComp.IsActive)
        {
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            return parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);
        }

        parms.spawnCenter = stargateOnMap.Position;
        return true;
    }
}
