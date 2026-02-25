using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;

/// <summary>
/// Allows colonists to find and eat food from mobile containers, even if the food isn't spawned in the world.
/// </summary>
[HarmonyPatch(typeof(FoodUtility), "SpawnedFoodSearchInnerScan")]
public static class Harmony_FoodUtility_SpawnedFoodSearchInnerScan
{
    [HarmonyPostfix]
    public static void Postfix(
        Pawn eater,
        IntVec3 root,
        PathEndMode peMode,
        TraverseParms traverseParams,
        float maxDistance,
        Predicate<Thing> validator,
        ref Thing __result)
    {
        if (__result != null)
            return;

        Pawn pawn = traverseParams.pawn ?? eater;
        if (pawn?.Map == null || !pawn.IsColonistPlayerControlled)
            return;

        List<IHaulSource> allSources = pawn.Map.haulDestinationManager?.AllHaulSourcesListForReading;
        if (allSources == null || allSources.Count == 0)
            return;

        List<Thing> mobileSources = [];
        for (int i = 0; i < allSources.Count; i++)
        {
            // Only consider enabled mobile containers with items inside to avoid unnecessary pathfinding checks
            if (allSources[i] is Building_MobileContainer cart && cart.HaulSourceEnabled && cart.AnythingToSearch)
                mobileSources.Add(cart);
        }

        LogUtil.Debug($"{pawn.Label} found {mobileSources.Count} mobile containers to search for food.");

        if (mobileSources.Count == 0)
            return;

        bool SafeFoodValidator(Thing t)
        {
            if (t is not Building_NutrientPasteDispenser && !t.def.IsNutritionGivingIngestible)
                return false;

            return validator == null || validator(t);
        }

        float PriorityGetter(Thing t)
        {
            ThingDef finalDef = FoodUtility.GetFinalIngestibleDef(t);
            int dist = (root - t.PositionHeld).LengthManhattan;
            return FoodUtility.FoodOptimality(eater, t, finalDef, dist);
        }

        Thing fallback = GenClosest.ClosestThing_Global_Reachable(
            root,
            pawn.Map,
            mobileSources,
            peMode,
            traverseParams,
            maxDistance,
            SafeFoodValidator,
            PriorityGetter,
            canLookInHaulableSources: true);

        if (fallback != null)
            __result = fallback;
    }
}
