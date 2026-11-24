using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(Designator_Strip), "CanDesignateThing")]
public static class Harmony_Designator_Strip
{
    public static void Postfix(ref AcceptanceReport __result, Thing t)
    {
        if (t is Pawn pawn && pawn?.ParentHolder is Building_Sarcophagus)
            __result = false;
    }
}
