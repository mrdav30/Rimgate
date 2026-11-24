using HarmonyLib;
using UnityEngine;
using Verse;

namespace Rimgate;

[HarmonyPatch(typeof(Projectile), "CheckForFreeInterceptBetween")]
public class Harmony_Projectile_CheckForInterceptBetween
{
    [HarmonyPostfix]
    public static void Postfix(
      Projectile __instance,
      ref bool __result,
      Vector3 lastExactPos,
      Vector3 newExactPos)
    {
        if (__result)
            return;

        var shieldGenList = __instance.Map?.GetComponent<MapComp_ShieldList>()?.ShieldGenList;
        if (shieldGenList == null)
            return;

        for (int index = 0; index < shieldGenList.Count; ++index)
        {
            var comp = shieldGenList[index]?.TryGetComp<Comp_ShieldEmitter>();
            if (comp == null) continue;
            bool intercepted = comp.CheckIntercept(__instance, lastExactPos, newExactPos);
            if (intercepted)
            {
                __instance.Destroy(DestroyMode.Vanish);
                __result = true;
                break;
            }
        }
    }
}