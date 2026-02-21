using HarmonyLib;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;

// passability of gate is impassable, so we set a very low cost on the vortex entry cell to allow pathing through it
[HarmonyPatch(typeof(PathGrid))]
public static class Harmony_PathGrid
{
    [HarmonyPatch(typeof(PathGrid), nameof(PathGrid.CalculatedCostAt))]
    [HarmonyPostfix]
    public static void Postfix(ref int __result,
        PathGrid __instance,
        IntVec3 c,
        bool perceivedStatic,
        IntVec3 prevCell,
        int? baseCostOverride = null)
    {
        Map map = __instance.map;
        if (map == null || map.Tile == PlanetTile.Invalid) return;

        if (!Building_Gate.GlobalVortexEntryCellCache.TryGetValue(map.uniqueID, out var cellIndex))
            return;

        int idx = map.cellIndices.CellToIndex(c);
        if (idx == cellIndex)
        {
            LogUtil.Debug($"Reducing cost for vortex entry cell {c} on map {map} at tile {map.Tile} from {__result} to 3.");

            __result = 3;
        }
    }
}