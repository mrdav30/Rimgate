using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;


[HarmonyPatch(typeof(WorkGiver_RescueDowned), nameof(WorkGiver_RescueDowned.HasJobOnThing))]
public static class Harmony_WorkGiver_RescueDowned_HasJobOnThing
{
    public static bool Prefix(ref bool __result, Pawn pawn, Thing t)
    {
        Pawn patient = t as Pawn;

        if (patient.ParentHolder is Building_Sarcophagus)
        {
            __result = false;
            return false;
        }


        var sarcophagus = SarcophagusUtility.FindBestSarcophagus(patient, pawn);
        if (sarcophagus != null)
        {
            __result = true;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(WorkGiver_RescueDowned), nameof(WorkGiver_RescueDowned.JobOnThing))]
public static class Harmony_WorkGiver_RescueDowned_JobOnThing
{
    public static bool Prefix(ref Job __result, Pawn pawn, Thing t)
    {
        Pawn patient = t as Pawn;

        if (patient.ParentHolder is Building_Sarcophagus)
        {
            __result = null;
            return false;
        }

        var sarcophagus = SarcophagusUtility.FindBestSarcophagus(patient, pawn);
        if(sarcophagus != null)
        {
            Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_RescueToSarcophagus, patient, sarcophagus);
            job.count = 1;

            __result = job;
            return false;
        }

        return true;
    }
}
