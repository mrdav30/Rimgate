using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(ThingOwnerUtility), nameof(ThingOwnerUtility.ContentsSuspended))]
public static class Harmony_ThingOwnerUtility
{
    [HarmonyPrefix]
    [HarmonyPriority(700)]
    public static bool Prefix(ref bool __result, IThingHolder holder)
    {
        for (IThingHolder ithingHolder = holder; ithingHolder != null; ithingHolder = ithingHolder.ParentHolder)
        {
            if (ithingHolder is Building_CloningPod)
            {
                __result = true;
                return false;
            }
        }
        return true;
    }
}
