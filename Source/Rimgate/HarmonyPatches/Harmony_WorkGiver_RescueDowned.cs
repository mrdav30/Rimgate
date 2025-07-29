using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;

// Switch out rescue job if heading to a sarcophagus
[HarmonyPatch(typeof(WorkGiver_RescueDowned), nameof(WorkGiver_RescueDowned.JobOnThing))]
public static class Harmony_WorkGiver_RescueDowned
{
    public static void Postfix(
        ref Job __result,
        Pawn pawn,
        Thing t,
        bool forced = false)
    {
        if (__result != null && t is Building_Bed_Sarcophagus bed)
            __result.def = Rimgate_DefOf.Rimgate_RescueToSarcophagus;
    }
}
