using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(Skyfaller), "Tick")]
public class Patch_Skyfaller_Tick
{
    [HarmonyPrefix]
    public static bool Prefix(Skyfaller __instance)
    {
        if (__instance.Map != null && __instance.ticksToImpact == 20)
        {
            Faction faction = __instance.Faction;
            if (faction == null || faction.HostileTo(Faction.OfPlayer))
            {
                var shieldGenList = __instance.Map?.GetComponent<MapComp_ShieldList>()?.ShieldGenList;
                if (shieldGenList == null)
                    return true;

                for (int index = 0; index < shieldGenList.Count; ++index)
                {
                    var comp = shieldGenList[index]?.TryGetComp<Comp_ShieldEmitter>();
                    if (comp == null) continue;
                    bool intercepted = comp.CheckIntercept(__instance);
                    if (intercepted)
                    {
                        __instance.Destroy(DestroyMode.KillFinalize);
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
