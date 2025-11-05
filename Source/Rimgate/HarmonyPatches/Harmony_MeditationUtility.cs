using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(MeditationUtility), nameof(MeditationUtility.GetMeditationJob))]
public static class Harmony_MeditationUtility
{
    public static void Postfix(Pawn pawn, bool forJoy, ref Job __result)
    {
        if (__result == null)
            return;

        // Only interested in normal meditation jobs
        if (__result.def != JobDefOf.Meditate && __result.def != JobDefOf.MeditatePray)
            return;

        Thing focusThing = __result.targetC.Thing;
        if (focusThing == null)
            return;

        // Only handle the Goa'uld throne
        if (focusThing.def != RimgateDefOf.Rimgate_GoauldThrone)
            return;

        // only replace if this pawn is the *assigned* one
        if (focusThing is Building building)
        {
            CompAssignableToPawn comp = building.TryGetComp<CompAssignableToPawn>();
            if (comp == null)
                return;

            // Skip if it's unassigned or assigned to someone else
            if (comp.AssignedPawnsForReading.NullOrEmpty() ||
                !comp.AssignedPawnsForReading.Contains(pawn))
                return;
        }

        // At this point, this pawn is assigned to the throne → use our job
        Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_MeditateOnGoauldThrone, focusThing);
        job.ignoreJoyTimeAssignment = __result.ignoreJoyTimeAssignment;

        __result = job;
    }
}
