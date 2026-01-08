using Rimgate;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_Warden_RescueToSarcophagus : WorkGiver_Warden
{
    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (pawn?.Map == null) return true;

        if (base.ShouldSkip(pawn, forced)) return true;

        if (pawn.IncapableOfHauling(out _)) return true;

        if (!pawn.Map.listerBuildings.ColonistsHaveBuilding((Thing building) => building is Building_Sarcophagus))
            return true;

        List<Pawn> list = pawn.Map.mapPawns?.SpawnedPawnsInFaction(pawn.Faction);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Downed && !list[i].InBed())
                return false;
        }

        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Pawn warden = pawn;
        Pawn prisoner = t as Pawn;

        if (prisoner == null) return null;

        if (!ShouldTakeCareOfPrisoner(pawn, prisoner))
            return null;

        if (!prisoner.Downed || prisoner.GetPosture().InBed())
            return null;

        if (prisoner.ParentHolder is Building_Sarcophagus)
            return null;

        if (!warden.CanReserve(prisoner))
            return null;

        Building_Sarcophagus sarcophagus = SarcophagusUtil.FindBestSarcophagus(prisoner, warden);
        if (sarcophagus != null)
        {
            Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_RescueToSarcophagus, prisoner, sarcophagus);
            job.count = 1;
            return job;
        }

        return null;
    }
}
