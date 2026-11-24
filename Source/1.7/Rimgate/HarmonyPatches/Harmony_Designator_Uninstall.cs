using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(Designator_Uninstall), nameof(Designator_Uninstall.CanDesignateThing))]
public static class Harmony_BlockGateUninstall
{
    public static void Postfix(Thing t, ref AcceptanceReport __result)
    {
        if (!__result.Accepted) return;
        bool flag = t is Rimgate.Building_Gate sg && (sg.IsActive || sg.ExternalHoldCount > 0);
        if (flag)
            __result = "RG_GateHeldCannotUninstall".Translate(t.LabelCap);
    }
}
