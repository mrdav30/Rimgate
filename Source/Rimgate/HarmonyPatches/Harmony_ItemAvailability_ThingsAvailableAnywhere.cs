using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate.HarmonyPatches;

/// <summary>
/// Ensures that the item availability check for construction resource delivery jobs considers mobile containers as valid sources,
/// by checking them for the requested items and updating the result accordingly.
/// </summary>
[HarmonyPatch(typeof(ItemAvailability), nameof(ItemAvailability.ThingsAvailableAnywhere))]
public static class Harmony_ItemAvailability_ThingsAvailableAnywhere
{
    private static readonly AccessTools.FieldRef<ItemAvailability, Map> MapField
        = AccessTools.FieldRefAccess<ItemAvailability, Map>("map");

    private static readonly AccessTools.FieldRef<ItemAvailability, Dictionary<int, bool>> CacheField
        = AccessTools.FieldRefAccess<ItemAvailability, Dictionary<int, bool>>("cachedResults");

    [HarmonyPostfix]
    public static void Postfix(
        ItemAvailability __instance,
        ThingDef need,
        int amount,
        Pawn pawn,
        ref bool __result)
    {
        if (__result
            || !MobileContainerHarmonyState.InConstructionResourceSearch
            || need == null
            || pawn == null
            || amount <= 0
            || (!pawn.IsColonistPlayerControlled && !pawn.IsColonyMechPlayerControlled))
            return;

        Map map = MapField(__instance) ?? pawn.Map;
        if (map == null || !HasViableMobileHaulSource(map))
            return;

        LogUtil.Debug($"Rimgate :: Checking haul sources for {pawn.Name} searching for {amount}x {need.label}");

        int total = 0;

        List<Thing> spawned = map.listerThings?.ThingsOfDef(need);
        if (spawned != null)
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                Thing thing = spawned[i];
                if (!thing.IsForbidden(pawn))
                {
                    total += thing.stackCount;
                    if (total >= amount)
                    {
                        SetTrueAndCache(__instance, need, pawn, ref __result);
                        return;
                    }
                }
            }
        }

        List<IHaulSource> sources = map.haulDestinationManager?.AllHaulSourcesListForReading;
        if (sources == null)
            return;

        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i] is not Building_MobileContainer cart || !cart.HaulSourceEnabled)
                continue;

            ThingOwner_MobileContainer container = cart.InnerContainer;
            if (container == null)
                continue;

            for (int j = 0; j < container.Count; j++)
            {
                Thing thing = container[j];
                if (thing == null || thing.def != need || thing.IsForbidden(pawn))
                    continue;

                total += thing.stackCount;
                if (total >= amount)
                {
                    SetTrueAndCache(__instance, need, pawn, ref __result);
                    return;
                }
            }
        }
    }

    private static void SetTrueAndCache(ItemAvailability instance, ThingDef need, Pawn pawn, ref bool result)
    {
        result = true;

        Dictionary<int, bool> cache = CacheField(instance);
        if (cache != null)
        {
            int key = Gen.HashCombine(need.GetHashCode(), pawn.Faction);
            cache[key] = true;
        }
    }

    private static bool HasViableMobileHaulSource(Map map)
    {
        List<IHaulSource> sources = map.haulDestinationManager?.AllHaulSourcesListForReading;
        if (sources == null)
            return false;

        for (int i = 0; i < sources.Count; i++)
        {
            // Check if the haul source is an enabled mobile container with contents to search
            if (sources[i] is Building_MobileContainer cart && cart.HaulSourceEnabled && cart.AnythingToSearch)
                return true;
        }

        return false;
    }
}
