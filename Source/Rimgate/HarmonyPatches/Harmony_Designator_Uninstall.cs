using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(Designator_Uninstall), nameof(Designator_Uninstall.CanDesignateThing))]
static class Patch_BlockStargateUninstall
{
    static void Postfix(Thing t, ref AcceptanceReport __result)
    {
        if (!__result.Accepted) return;
        bool flag = t is Rimgate.Building_Stargate sg 
            && sg.StargateControl != null 
            && (sg.StargateControl.IsActive 
                || sg.StargateControl.ExternalHoldCount > 0);
        if (flag)
            __result = "RG_StargateHeldCannotUninstall".Translate();
    }
}
