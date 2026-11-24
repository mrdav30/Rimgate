using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;

/// <summary>
/// Allows colonists to eat food directly from mobile containers, even if the food isn't spawned in the world.
/// </summary>
[HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
public static class Harmony_JobGiver_GetFood
{
    [HarmonyPostfix]
    public static void Postfix(Pawn pawn, ref Job __result)
    {
        if (__result == null || __result.def != JobDefOf.Ingest || !__result.targetA.HasThing)
            return;

        Thing food = __result.targetA.Thing;
        if (food == null || food.Spawned)
            return;

        if (food.ParentHolder is not Building_MobileContainer container)
            return;

        LogUtil.Debug($"{pawn.Label} found non-spawned food {food} in mobile container {container}");

        if (!container.AllowColonistsUseContents)
            return;

        Job replacement = JobMaker.MakeJob(RimgateDefOf.Rimgate_IngestFromMobileContainer, food, container);
        replacement.count = __result.count;
        replacement.playerForced = __result.playerForced;

        __result = replacement;
    }
}
