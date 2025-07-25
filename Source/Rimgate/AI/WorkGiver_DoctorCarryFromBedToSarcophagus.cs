using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_DoctorCarryFromBedToSarcophagus
    : WorkGiver_DoctorRescueToSarcophagus
{
    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        List<Pawn> list = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].InBed())
                return false;
        }

        return true;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Pawn patient = t as Pawn;

        if (patient == null 
            || patient == pawn 
            || !patient.InBed() 
            || patient.CurrentBed()?.def.thingClass == typeof(Building_Bed_Sarcophagus)
            || patient.health.surgeryBills.Bills.Any(x => x.suspended == false) 
            || !pawn.CanReserve(patient, 1, -1, null, forced) 
            || GenAI.EnemyIsNear(patient, MinDistFromEnemy))
        {
            return false;
        }

        Building_Bed_Sarcophagus bedSarcophagus = SarcophagusRestUtility.FindBestSarcophagus(pawn, patient);
        if (bedSarcophagus != null 
            && SarcophagusHealthAIUtility.ShouldSeekSarcophagusRest(patient, bedSarcophagus) 
            && SarcophagusHealthAIUtility.HasAllowedMedicalCareCategory(patient) 
            && patient.CanReserve(bedSarcophagus))
        {
            return true;
        }

        return false;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Pawn patient = t as Pawn;
        Building_Bed_Sarcophagus bedSarcophagus = SarcophagusRestUtility.FindBestSarcophagus(pawn, patient);
        Job job = JobMaker.MakeJob(Rimgate_DefOf.Rimgate_CarryToSarcophagus, patient, bedSarcophagus);
        job.count = 1;
        return job;
    }
}
