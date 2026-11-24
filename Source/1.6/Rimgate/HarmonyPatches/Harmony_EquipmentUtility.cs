using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch]
public static class Harmony_EquipmentUtility
{
    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(EquipmentUtility),
            "CanEquip",
            new Type[4]
            {
                typeof(Thing),
                typeof(Pawn),
                typeof(string).MakeByRefType(),
                typeof(bool)
            });
    }

    public static void Postfix(ref bool __result, Thing thing, Pawn pawn, ref string cantReason, bool checkBonded = true)
    {
        if (thing is not Apparel apparel) return;

        Comp_FrameApparel comp = ThingCompUtility.TryGetComp<Comp_FrameApparel>(apparel);
        if (comp == null || !comp.IsFrameComponent || comp.HasAnyRequiredRootEquipped(pawn)) return;

        cantReason = comp.Props.failReasonKey.Translate();
        __result = false;
    }
}
