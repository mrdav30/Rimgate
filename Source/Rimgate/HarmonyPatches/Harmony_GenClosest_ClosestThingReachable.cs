using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;

/// <summary>
/// Enables searching mobile containers as haul sources during construction resource delivery jobs, 
/// by setting the 'lookInHaulSources' flag in GenClosest.ClosestThingReachable when appropriate.
/// </summary>
[HarmonyPatch(typeof(GenClosest), nameof(GenClosest.ClosestThingReachable))]
public static class Harmony_GenClosest_ClosestThingReachable
{
    [HarmonyPrefix]
    public static void Prefix(
        IntVec3 root,
        Map map,
        ThingRequest thingReq,
        PathEndMode peMode,
        TraverseParms traverseParams,
        float maxDistance,
        Predicate<Thing> validator,
        IEnumerable<Thing> customGlobalSearchSet,
        int searchRegionsMin,
        int searchRegionsMax,
        bool forceAllowGlobalSearch,
        RegionType traversableRegionTypes,
        bool ignoreEntirelyForbiddenRegions,
        ref bool lookInHaulSources)
    {
        if (lookInHaulSources || map == null || !MobileContainerHarmonyState.InConstructionResourceSearch)
            return;

        Pawn pawn = traverseParams.pawn;
        if (pawn == null || (!pawn.IsColonistPlayerControlled && !pawn.IsColonyMechPlayerControlled))
            return;

        if (thingReq.singleDef == null || !thingReq.singleDef.EverHaulable)
            return;

        if (!HasEnabledMobileHaulSource(map))
            return;

        LogUtil.Debug($"Rimgate :: Enabling haul source search for {pawn.Name} searching for {thingReq.singleDef.label}");

        lookInHaulSources = true;
    }

    private static bool HasEnabledMobileHaulSource(Map map)
    {
        List<IHaulSource> sources = map.haulDestinationManager?.AllHaulSourcesListForReading;
        if (sources == null)
            return false;

        for (int i = 0; i < sources.Count; i++)
        {
            // check if the source is a mobile container with haul source enabled, and if it has anything to search
            if (sources[i] is Building_MobileContainer cart && cart.HaulSourceEnabled && cart.AnythingToSearch)
                return true;
        }

        return false;
    }
}
