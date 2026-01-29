using HarmonyLib;
using System;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(
    typeof(MechanitorUtility),
    nameof(MechanitorUtility.InMechanitorCommandRange),
    new Type[] { 
        typeof(Pawn), 
        typeof(LocalTargetInfo)
    })]
static class PatchMechanitorUtility_InMechanitorCommandRange
{
    static bool Prefix(
        Pawn mech,
        LocalTargetInfo target,
        ref bool __result
        )
    {
        if (mech != null && mech.def == RimgateDefOf.Rimgate_Malp)
        {
            __result = true;
            return false;
        }
        return true;
    }
}
