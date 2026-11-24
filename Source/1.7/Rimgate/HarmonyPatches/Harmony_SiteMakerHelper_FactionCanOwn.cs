using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace Rimgate.HarmonyPatches;

// we could override FactionCanOwn via SitePartDef, but this is simpler
[HarmonyPatch(
    typeof(SiteMakerHelper),
    "FactionCanOwn",
    [
        typeof (SitePartDef),
        typeof (Faction),
        typeof (bool),
        typeof (Predicate<Faction>)
    ])]
public static class Harmony_SiteMakerHelper_FactionCanOwn
{
    public static readonly List<FactionDef> RimgateHiddenFactions =
    [
        RimgateDefOf.Rimgate_Replicator,
        RimgateDefOf.Rimgate_TreasureHunters
    ];

    public static bool Prefix(
        ref bool __result,
        SitePartDef sitePart,
        Faction faction)
    {
        // Allow Rimgate hidden factions to own sites

        if (faction == null || !RimgateHiddenFactions.Contains(faction.def))
        {
            LogUtil.Debug($"Preventing faction '{faction?.Name ?? "null"}' from owning site part '{sitePart.defName}'");
            return true;
        }

        LogUtil.Debug($"Allowing faction '{faction?.Name ?? "null"}' to own site part '{sitePart.defName}'");

        __result = true;
        return false;
    }
}
