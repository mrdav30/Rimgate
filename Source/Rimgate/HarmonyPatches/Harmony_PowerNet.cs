using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimgate.HarmonyPatches;

// Blocks the grid from charging ZPM batteries
[HarmonyPatch(typeof(PowerNet), "DistributeEnergyAmongBatteries")]
public static class Harmony_BlockZpmFromBatteryDistribution
{
    private static readonly FieldInfo BatteryCompsField =
        AccessTools.Field(typeof(PowerNet), "batteryComps");

    // Remember the original list per-net so we can restore post-call
    private static readonly Dictionary<PowerNet, List<CompPowerBattery>> SavedLists = new();

    static void Prefix(PowerNet __instance, ref float energy)
    {
        // Nothing to do if no energy or field not found
        if (energy <= 0f || BatteryCompsField == null) return;

        var batteries = (List<CompPowerBattery>)BatteryCompsField.GetValue(__instance);
        if (batteries == null || batteries.Count == 0) return;

        // Save original reference
        SavedLists[__instance] = batteries;

        // Build filtered list that excludes ZPM batteries
        var filtered = batteries.Where(b => 
            b?.parent?.def != RimgateDefOf.Rimgate_ZPM
            && b?.parent?.def != RimgateDefOf.Rimgate_ZPMHousing
        ).ToList();

        // Swap in filtered list for the duration of the original method
        BatteryCompsField.SetValue(__instance, filtered);
    }

    static void Postfix(PowerNet __instance)
    {
        if (BatteryCompsField == null) return;

        // Restore original list if we replaced it
        if (SavedLists.TryGetValue(__instance, out var original))
        {
            BatteryCompsField.SetValue(__instance, original);
            SavedLists.Remove(__instance);
        }
    }
}