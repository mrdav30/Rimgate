using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace Rimgate.HarmonyPatches;

/// <summary>
/// Sets a flag indicating that a construction resource search is in progress, allowing other patches to modify their behavior accordingly.
/// </summary>
[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "ResourceDeliverJobFor")]
public static class Harmony_WorkGiver_ConstructDeliverResources_ResourceDeliverJobFor
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        MobileContainerHarmonyState.EnterConstructionResourceSearch();
    }

    [HarmonyFinalizer]
    public static Exception Finalizer(Exception __exception)
    {
        MobileContainerHarmonyState.ExitConstructionResourceSearch();
        return __exception;
    }
}
