using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_Doctor_RescueToSarcophagus : WorkGiver_RescueDowned
{
    protected const float MinDistFromEnemy = 40f;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (pawn?.Map == null) return true;

        if (Utils.PawnIncapableOfHauling(pawn, out _)) return true;

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

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (pawn?.Map == null) return false;

        if (!pawn.Map.listerBuildings.ColonistsHaveBuilding((Thing building) => building is Building_Sarcophagus))
            return false;

        Pawn patient = t as Pawn;

        if (patient == null
            || patient.GetUniqueLoadID() == pawn.GetUniqueLoadID()
            || !patient.Downed
            || patient.health.hediffSet.InLabor()
            || patient.GetPosture().InBed()
            || patient.Faction != pawn.Faction
            || !MedicalUtil.HasAllowedMedicalCareCategory(patient)
            || patient.ParentHolder is Building_Sarcophagus
            || !pawn.CanReserve(patient, 1, -1, null, forced)
            || GenAI.EnemyIsNear(patient, MinDistFromEnemy))
        {
            return false;
        }

        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Pawn patient = t as Pawn;
        Building_Sarcophagus sarcophagus = SarcophagusUtil.FindBestSarcophagus(patient, pawn);
        if (sarcophagus != null)
        {
            Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_RescueToSarcophagus, patient, sarcophagus);
            job.count = 1;
            return job;
        }

        return null;
    }
}