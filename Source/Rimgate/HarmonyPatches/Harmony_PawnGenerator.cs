using HarmonyLib;
using RimWorld;
using VEF.Utils;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(PawnGenerator), "GeneratePawnRelations")]
public static class Harmony_PawnGenerator
{
    [HarmonyPostfix]
    public static void GeneratePawnRelationsPatch(Pawn pawn, ref PawnGenerationRequest request)
    {
        if (pawn.relations == null)
            return;

        foreach (Pawn relative in pawn.relations.FamilyByBlood)
        {
            if (relative == null
                || !(relative.Name is NameTriple name)
                || !Utils.HasHediff<Hediff_Clone>(pawn))
                break;

            foreach (PawnRelationDef relation in PawnRelationUtility.GetRelations(pawn, relative))
            {
                if (relation != null)
                {
                    if (!relation.implied)
                        pawn.Destroy(DestroyMode.Vanish);
                }
                else
                    break;
            }
        }
    }
}

