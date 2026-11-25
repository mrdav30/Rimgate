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

[HarmonyPatch(typeof(ThingOwnerUtility), nameof(ThingOwnerUtility.TryGetInnerInteractableThingOwner))]
public static class Harmony_ThingOwnerUtility_GateTransporter
{
    [HarmonyPostfix]
    public static void Postfix(Thing __0, ref ThingOwner __result)
    {
        // Native hauling must notify the transporter, not deposit into transit's pending owner.
        if (__0 is Building_Gate gate && gate.Transporter?.innerContainer is ThingOwner container)
            __result = container;
    }
}
