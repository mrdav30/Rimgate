using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Rimgate.HarmonyPatches;

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
public static class Harmony_SiteMakerHelper
{
    public static bool Prefix(
        ref bool __result,
        SitePartDef sitePart,
        Faction faction)
    {
        if (!sitePart.tags.Contains(RimgateMod.StargateQuestTag))
            return true;

        // Allow Rimgate hidden factions to own sites
        if (faction == null) return true;

        if (faction.def != RimgateDefOf.Rimgate_Replicator
            && faction.def != RimgateDefOf.Rimgate_TreasureHunters
            && faction.def != RimgateDefOf.Rimgate_TreasureHuntersHostile)
            return true;

        __result = true;
        return false;
    }
}
