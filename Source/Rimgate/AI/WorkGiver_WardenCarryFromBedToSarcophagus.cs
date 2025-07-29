using Rimgate;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_WardenCarryFromBedToSarcophagus : WorkGiver_Warden
{
    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Pawn warden = pawn;
        Pawn prisoner = t as Pawn;

        if (!ShouldTakeCareOfPrisoner(pawn, prisoner))
            return null;

        if (!prisoner.InBed())
            return null;

        if (prisoner.CurrentBed()?.def.thingClass == typeof(Building_Bed_Sarcophagus))
            return null;

        if (prisoner.health.surgeryBills.Bills.Any(x => x.suspended == false))
            return null;

        if (!warden.CanReserve(prisoner))
            return null;

        Building_Bed_Sarcophagus bed = SarcophagusRestUtility.FindBestSarcophagus(warden, prisoner);
        if (bed != null
            && SarcophagusHealthAIUtility.ShouldSeekSarcophagusRest(prisoner, bed)
            && SarcophagusHealthAIUtility.HasAllowedMedicalCareCategory(prisoner)
            && prisoner.CanReserve(bed))
        {
            Job job = JobMaker.MakeJob(Rimgate_DefOf.Rimgate_CarryToSarcophagus, prisoner, bed);
            job.count = 1;
            return job;
        }

        return null;
    }
}
