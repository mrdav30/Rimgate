using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(Designator_Strip), "CanDesignateThing")]
public static class Harmony_Designator_Strip_CanDesignateThing
{
    static void Postfix(ref AcceptanceReport __result, Thing t)
    {
        if (t is Pawn pawn 
            && !pawn.Dead 
            && pawn.CurrentBed() is Building_Bed_Sarcophagus)
        {
            __result = false;
        }
    }
}
