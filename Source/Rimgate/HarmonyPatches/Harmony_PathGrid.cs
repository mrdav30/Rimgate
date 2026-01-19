using HarmonyLib;
using Rimgate;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;

// <summary>
// Patch AvoidGrid to add avoidance for Stargate vortex cells
// </summary>
[HarmonyPatch(typeof(PathGrid))]
public static class Harmony_PathGrid
{
    [HarmonyPatch(typeof(PathGrid), nameof(PathGrid.CalculatedCostAt))]
    [HarmonyPostfix]
    public static void Postfix(ref int __result, PathGrid __instance, IntVec3 c, bool perceivedStatic, IntVec3 prevCell, int? baseCostOverride = null)
    {
        Map map = __instance.map;
        if (map == null || map.Tile == PlanetTile.Invalid) return;

        if (!Building_Stargate.GlobalVortexCellsCache.TryGetValue(map.uniqueID, out var block))
            return;

        if (!block.Active) return; // only during danger window

        int idx = map.cellIndices.CellToIndex(c);
        if (block.CellIndices.Contains(idx))
        {
            if (RimgateMod.Debug)
                Log.Message($"PathGrid.CalculatedCostAt: Increasing cost for vortex cell {c} on map {map} at tile {map.Tile}");
            // "almost impassable" but still technically passable if no other route
            __result = PathGrid.ImpassableCost - 1;
        }
    }
}