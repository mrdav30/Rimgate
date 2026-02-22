using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized))]
public static class Harmony_StatWorker_SymbioteQueenOffsets
{
    private static readonly System.Reflection.FieldInfo StatField =
        AccessTools.Field(typeof(StatWorker), "stat");

    public static void Postfix(StatWorker __instance, StatRequest req, ref float __result)
    {
        if (!req.HasThing || req.Thing is not Pawn pawn)
            return;

        HediffSet set = pawn.health?.hediffSet;
        if (set == null)
            return;

        if (!set.TryGetHediff(RimgateDefOf.Rimgate_SymbioteImplant, out Hediff hediff))
            return;

        if (hediff is not Hediff_SymbioteImplant implant)
            return;

        SymbioteQueenLineage lineage = implant.Heritage?.QueenLineage;
        if (lineage == null)
            return;

        if (StatField == null)
            return;

        if (StatField.GetValue(__instance) is not StatDef stat)
            return;

        float offset = lineage.GetOffset(stat);
        if (Mathf.Abs(offset) <= 0.0001f)
            return;

        __result += offset;
    }
}
