using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate.HarmonyPatches;

/// <summary>
/// Adds the resources in enabled mobile containers to the resource readout for build ghosts.
/// </summary>
[HarmonyPatch(typeof(ResourceCounter), nameof(ResourceCounter.GetCount))]
public static class Harmony_ResourceCounter_GetCount
{
    private static readonly AccessTools.FieldRef<ResourceCounter, Map> MapField
        = AccessTools.FieldRefAccess<ResourceCounter, Map>("map");

    [HarmonyPostfix]
    public static void Postfix(ResourceCounter __instance, ThingDef rDef, ref int __result)
    {
        if (!MobileContainerHarmonyState.InBuildGhostResourceReadout || rDef == null || !rDef.CountAsResource)
            return;

        Map map = MapField(__instance);
        if (map == null)
            return;

        __result += CountInEnabledMobileContainers(map, rDef);
    }

    private static int CountInEnabledMobileContainers(Map map, ThingDef def)
    {
        List<IHaulSource> sources = map.haulDestinationManager?.AllHaulSourcesListForReading;
        if (sources == null)
            return 0;

        int total = 0;
        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i] is not Building_MobileContainer cart
                || !cart.HaulSourceEnabled
                || cart.Faction != Faction.OfPlayer)
            {
                continue;
            }

            ThingOwner_MobileContainer container = cart.InnerContainer;
            if (container == null || container.Count == 0)
                continue;

            for (int j = 0; j < container.Count; j++)
            {
                Thing thing = container[j];
                if (thing != null && thing.def == def)
                    total += thing.stackCount;
            }
        }

        return total;
    }
}
