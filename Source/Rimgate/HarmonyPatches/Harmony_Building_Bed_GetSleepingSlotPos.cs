using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.GetSleepingSlotPos))]
public static class Harmony_Building_Bed_GetSleepingSlotPos
{
    public static void Postfix(ref IntVec3 __result, ref Building_Bed __instance)
    {
        if (__instance.def.thingClass == typeof(Building_Bed_Sarcophagus))
            __result = __instance.Position;
    }
}
