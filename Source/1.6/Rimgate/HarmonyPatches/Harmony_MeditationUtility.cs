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

        // Only normal meditation jobs
        if (__result.def != JobDefOf.Meditate && __result.def != JobDefOf.MeditatePray)
            return;

        if (pawn.Map == null)
            return;

        // 1) Try to find an assigned Goa'uld throne for this pawn
        Building focus = GetAssignedMeditationSpot(pawn, RimgateDefOf.Rimgate_GoauldThrone);

        // 2) Try to find an assigned Wraith table for this pawn
        if (focus == null)
            focus = GetAssignedMeditationSpot(pawn, RimgateDefOf.Rimgate_WraithTable);

        if (focus == null)
            return;

        // Make sure we can actually use this focus safely
        IntVec3 standCell = focus.InteractionCell;
        Map map = pawn.Map;

        if (!standCell.InBounds(map)
            || !standCell.Standable(map)
            || standCell.IsForbidden(pawn)
            || !map.reachability.CanReach(pawn.Position, standCell, PathEndMode.OnCell, TraverseParms.For(pawn))
            || !MeditationUtility.SafeEnvironmentalConditions(pawn, standCell, map))
        {
            // If the assigned focus is unusable, fall back to vanilla behavior
            return;
        }

        // Build the appropriate custom job
        Job job;
        if (focus.def == RimgateDefOf.Rimgate_GoauldThrone)
            job = JobMaker.MakeJob(RimgateDefOf.Rimgate_MeditateOnGoauldThrone, focus);
        else // Rimgate_WraithTable
            job = JobMaker.MakeJob(RimgateDefOf.Rimgate_MeditateAtWraithTable, focus);

        job.ignoreJoyTimeAssignment = __result.ignoreJoyTimeAssignment;
        __result = job;
    }

    private static Building GetAssignedMeditationSpot(Pawn pawn, ThingDef def)
    {
        if (pawn.Map == null)
            return null;

        var buildings = pawn.Map.listerBuildings.AllBuildingsColonistOfDef(def);
        foreach (var b in buildings)
        {
            var comp = b.TryGetComp<CompAssignableToPawn>();
            if (comp != null
                && !comp.AssignedPawnsForReading.NullOrEmpty()
                && comp.AssignedPawnsForReading.Contains(pawn))
            {
                return b;
            }
        }

        return null;
    }
}
