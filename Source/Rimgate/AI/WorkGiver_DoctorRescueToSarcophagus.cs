using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_DoctorRescueToSarcophagus : WorkGiver_RescueDowned
{
    protected const float MinDistFromEnemy = 40f;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		Pawn patient = t as Pawn;

		if (patient == null 
			|| !patient.Downed 
			|| patient == pawn 
			|| patient.Faction != pawn.Faction 
			|| patient.CurrentBed()?.def.thingClass == typeof(Building_Bed_Sarcophagus)
			|| !pawn.CanReserve(patient, 1, -1, null, forced) 
			|| GenAI.EnemyIsNear(patient, MinDistFromEnemy))
		{
			return false;
		}

        Building_Bed_Sarcophagus bedSarcophagus = SarcophagusRestUtility.FindBestSarcophagus(pawn, patient);
		if (bedSarcophagus != null 
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
		Job job = JobMaker.MakeJob(Rimgate_DefOf.Rimgate_RescueToSarcophagus, patient, bedSarcophagus);
		job.count = 1;
		return job;
	}
}