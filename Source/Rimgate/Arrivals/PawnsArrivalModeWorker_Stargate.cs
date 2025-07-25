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
        Thing stargateOnMap = Comp_Stargate.GetStargateOnMap(map);

        Comp_Stargate sgComp = stargateOnMap.TryGetComp<Comp_Stargate>();
        sgComp.OpenStargateDelayed(-1, 450);
        sgComp.ticksSinceBufferUnloaded = -150;
        sgComp.isRecievingGate = true;
        foreach (Pawn pawn in pawns)
            sgComp.AddToRecieveBuffer(pawn);
    }

    public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
    {
        Map map = (Map)parms.target;
        parms.spawnRotation = Rot4.South;
        Thing stargateOnMap = Comp_Stargate.GetStargateOnMap(map);
        Comp_Stargate sgComp = stargateOnMap == null ? null : stargateOnMap.TryGetComp<Comp_Stargate>();

        bool isActive = stargateOnMap == null 
            || sgComp == null 
            || sgComp.stargateIsActive;
        if (isActive)
        {
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            return parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);
        }

        parms.spawnCenter = stargateOnMap.Position;
        return true;
    }
}
