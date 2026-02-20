using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Rimgate.HarmonyPatches;

// Prevent Doctors/Wardens from feeding patients if:
// - The patient is lying in a Sarcophagus
// - The Sarcophagus is powered
[HarmonyPatch]
public static class Harmony_FeedingUtilities
{
    [HarmonyTargetMethods]
    public static IEnumerable<MethodInfo> TargetMethods()
    {
        yield return AccessTools.Method(typeof(FeedPatientUtility), "ShouldBeFed");
        yield return AccessTools.Method(typeof(WardenFeedUtility), "ShouldBeFed");
    }

    // Run this before Dubs Bad Hygiene, so that the mod's associated
    // administer fluid jobs are skipped when the patient is in a Sarcophagus
    [HarmonyBefore(new string[] { "Dubwise.DubsBadHygiene" })]
    public static void Postfix(ref bool __result, Pawn p)
    {
        if (p.ParentHolder is Building_Sarcophagus sarcophagus
            && sarcophagus.Power?.PowerOn == true)
        {
            __result = false;
        }
    }
}
