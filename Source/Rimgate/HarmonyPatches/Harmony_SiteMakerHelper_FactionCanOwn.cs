using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Rimgate.HarmonyPatches;

// we could override FactionCanOwn via SitePartDef, but this is simpler
[HarmonyPatch(
    typeof(SiteMakerHelper),
    "FactionCanOwn",
    new Type[4]
    {
        typeof (SitePartDef),
        typeof (Faction),
        typeof (bool),
        typeof (Predicate<Faction>)
    })]
public static class Harmony_SiteMakerHelper_FactionCanOwn
{
    // TODO: make this configurable via mod settings
    public static readonly List<FactionDef> RimgateHiddenFactions = new()
    {
        RimgateDefOf.Rimgate_Replicator,
        RimgateDefOf.Rimgate_TreasureHunters
    };

    public static bool Prefix(
        ref bool __result,
        SitePartDef sitePart,
        Faction faction)
    {
        // Allow Rimgate hidden factions to own sites

        if (faction == null || !RimgateHiddenFactions.Contains(faction.def))
        {
            if(RimgateMod.Debug)
                Log.Message($"Rimgate :: Preventing faction '{faction?.Name ?? "null"}' from owning site part '{sitePart.defName}'");
            return true;
        }

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Allowing faction '{faction?.Name ?? "null"}' to own site part '{sitePart.defName}'");

        __result = true;
        return false;
    }
}
