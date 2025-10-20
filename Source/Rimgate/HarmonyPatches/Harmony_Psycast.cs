using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate.HarmonyPatches;

// allow wraith to use all psycast abilities
[HarmonyPatch(typeof(Ability), nameof(Ability.AICanTargetNow))]
public static class Harmony_Psycast
{
    public static bool Prefix(
        Ability __instance,
        LocalTargetInfo target,
        ref bool __result)
    {
        if (__instance is not Psycast || !__instance.pawn.IsXenoTypeOf(RimgateDefOf.Rimgate_Wraith)) return true;

        __result = false;
        if (!__instance.CanCast) return false;

        if (!__instance.CanApplyOn(target)) return false;

        if (__instance.EffectComps != null)
        {
            foreach (CompAbilityEffect effectComp in __instance.EffectComps)
            {
                if (!effectComp.AICanTargetNow(target))
                    return false;
            }
        }

        __result = true;
        return true;
    }
}
