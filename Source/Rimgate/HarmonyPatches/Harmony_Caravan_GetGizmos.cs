using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using static RimWorld.PsychicRitualRoleDef;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(Caravan), "GetGizmos")]
public class Harmony_Caravan_GetGizmos
{
    // Whitelist of Odyssey landmarks that play nicely with our transit site
    private static readonly HashSet<string> SafeLandmarks = new(StringComparer.OrdinalIgnoreCase)
    {
        "DryLake",
        "Wetland",
        "Valley",
        "Chasm",
        "Cliffs",
        "Hollow",
        "TerraformingScar",
        "Dunes",
        "Plateau",
        "Basin",
        "LavaLake",
        "LavaCrater",
        "LavaFlow",
        "Iceberg",
        "Fjord",
        "Cove",
        "Bay",
        "Peninsula",
        "CoastalIsland",
        "Archipelago",
        "HotSprings",
        "ToxicLake",
        "Pond",
        "LakeWithIsland",
        "LakeWithIslands",
        "Lake",
        "Oasis",
        "IceDunes",
        "AncientSmokeVent",
        "AncientToxVent",
        "AncientHeatVent"
    };

    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Caravan __instance)
    {
        foreach (var g in gizmos) yield return g;

        bool containsGate = __instance.AllThings.Any(t =>
            t.GetInnerIfMinified() is Building_Gate bs 
            && bs != null);

        var cmd = new Command_Action
        {
            icon = RimgateTex.TransitSiteCommandTex,
            action = () =>
            {
                // Remove exactly one Gate, then one DHD
                bool removedGate = RemoveFirstWithInner(__instance, RimgateDefOf.Rimgate_Dwarfgate);
                bool removedDhd = RemoveFirstWithInner(__instance, RimgateDefOf.Rimgate_DialHomeDevice);

                var wo = (WorldObject_GateTransitSite)
                    WorldObjectMaker.MakeWorldObject(RimgateDefOf.Rimgate_GateTransitSite);
                wo.Tile = __instance.Tile;

                // Record initial loadout for PostMapGenerate
                wo.InitState(removedGate || true, removedDhd);

                Find.WorldObjects.Add(wo);
            },
            defaultLabel = "RG_CreateTransitSite".Translate(),
            defaultDesc = "RG_CreateTransitSiteDesc".Translate()
        };

        StringBuilder disabledReason = new();
        if(GateUtil.AddressBookFull)
            disabledReason.Append("RG_Cannot_AddressBookFull".Translate());
        else if (!containsGate)
            disabledReason.Append("RG_NoGateInCaravan".Translate());
        else if (IsBlockedByLandmark(__instance.Tile, out var landmarkLabel))
            disabledReason.Append("RG_BlockedByLandmark".Translate(landmarkLabel));

        if (disabledReason.Length > 0 || !TileFinder.IsValidTileForNewSettlement(__instance.Tile, disabledReason))
        {
            string reason = disabledReason.Length > 0
                ? disabledReason.ToString().TrimEndNewlines()
                : "RG_InvalidTileForSettlement".Translate();
            cmd.Disable("RG_CannotCreateTransientSite".Translate(reason));
        }

        yield return cmd;
    }

    private static bool IsBlockedByLandmark(int tileId, out string label)
    {
        label = null;
        var tile = Find.WorldGrid[tileId];
        var lm = tile?.Landmark;
        if (lm == null) return false;

        label = lm.def?.label ?? lm.def?.defName ?? "landmark";

        // Block anything not on the safe list.
        // This automatically catches structure/complex landmarks introduced by Odyssey that cause gen-step issues.
        var defName = lm.def?.defName;
        if (string.IsNullOrEmpty(defName)) return true;

        return !SafeLandmarks.Contains(defName);
    }

    private static bool RemoveFirstWithInner(Caravan caravan, ThingDef def)
    {
        foreach (var outer in caravan.AllThings)
        {
            var inner = outer.GetInnerIfMinified();
            if (inner?.def == def)
            {
                outer.holdingOwner?.Remove(outer);
                return true;
            }
        }
        return false;
    }
}
