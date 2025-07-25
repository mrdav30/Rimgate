using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Rimgate.HarmonyPatches;

// Prevent Doctors/Wardens from feeding patients if:
// - The patient is lying on a Sarcophagus
// - The Sarcophagus is powered
[HarmonyPatch]
public static class Harmony_FeedingUtilities
{
    public static IEnumerable<MethodInfo> TargetMethods()
    {
        yield return AccessTools.Method(typeof(FeedPatientUtility), "ShouldBeFed");
        yield return AccessTools.Method(typeof(WardenFeedUtility), "ShouldBeFed");
    }

    // Run this before Dubs Bad Hygiene, so that the mod's associated
    // administer fluid jobs are skipped when the patient is on a Sarcophagus
    [HarmonyBefore(new string[] { "Dubwise.DubsBadHygiene" })]
    public static void Postfix(ref bool __result, Pawn p)
    {
        if (p.CurrentBed() is Building_Bed_Sarcophagus bedSarcophagus 
            && bedSarcophagus.powerComp.PowerOn)
        {
            __result = false;
        }
    }
}
