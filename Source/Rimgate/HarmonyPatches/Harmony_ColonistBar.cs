using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static RimWorld.ColonistBar;

namespace Rimgate.HarmonyPatches;

// ColonistBar caches a flat list of entries (pawns), grouped by map/caravan.
// We wait until it's rebuilt, then drop any entries belonging to a StargateSite
// map that has no “showable” pawns.
[HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
public static class Harmony_ColonistBar
{
    private static bool _wasDirty;

    private static HashSet<int> _tmpRemovals;

    private static Dictionary<int, int> _tmpRemovalsMap;

    public static bool Prefix(ColonistBar __instance)
    {
        var trav = Traverse.Create(__instance);
        if (!_wasDirty)
            _wasDirty = trav.Field("entriesDirty").GetValue<bool>();
        return true;
    }

    public static void Postfix(ColonistBar __instance)
    {
        // Only run right after a real recache
        if (!_wasDirty) return;
        _wasDirty = false;

        var trav = Traverse.Create(__instance);
        var entries = trav.Field("cachedEntries").GetValue<List<Entry>>();
        if (entries == null || entries.Count == 0) return;

        // 1) Hide Stargate site maps with no pawns and no active gate
        _tmpRemovals ??= new HashSet<int>();
        _tmpRemovals.Clear();

        foreach (var e in entries)
        {
            var m = e.map;
            if (m == null) continue;
            if (m.info?.parent is not WorldObject_QuestStargateSite) continue;

            bool showThisMap = Rimgate.StargateUtility.ActiveGateOnMap(m)
                || (m.mapPawns?.AnyPawnBlockingMapRemoval ?? false);

            if (!showThisMap)
                _tmpRemovals.Add(m.uniqueID);
        }

        if (_tmpRemovals.Count > 0)
            entries.RemoveAll(e => e.map != null && _tmpRemovals.Contains(e.map.uniqueID));

        // If nothing left, clear draw locs and bail
        if (entries.Count == 0)
        {
            trav.Field("cachedEntries").SetValue(entries);
            var drawLocsEmpty = trav.Field("cachedDrawLocs").GetValue<List<Vector2>>();
            drawLocsEmpty.Clear();
            trav.Field("cachedScale").SetValue(1f);
            return;
        }

        // 2) Densify existing group ids (preserve vanilla grouping incl. caravans)
        _tmpRemovalsMap ??= new Dictionary<int, int>(16);
        _tmpRemovalsMap.Clear();
        int nextGroup = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            int old = e.group;  // group assigned by vanilla (map, caravan, etc.)
            if (!_tmpRemovalsMap.TryGetValue(old, out var g))
            {
                g = nextGroup++;
                _tmpRemovalsMap[old] = g;
            }
            e.group = g;    // write back the dense group
            entries[i] = e; // struct write-back
        }

        // 3) Store filtered, re-indexed entries
        trav.Field("cachedEntries").SetValue(entries);

        // 4) Recompute draw locs using the actual number of groups left
        var drawLocs = trav.Field("cachedDrawLocs").GetValue<List<Vector2>>();
        drawLocs.Clear();
        float scale;
        new ColonistBarDrawLocsFinder().CalculateDrawLocs(drawLocs, out scale, _tmpRemovalsMap.Count);
        trav.Field("cachedDrawLocs").SetValue(drawLocs);
        trav.Field("cachedScale").SetValue(scale);
    }
}

