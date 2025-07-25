using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;

// Doctors should not perform scheduled surgeries on patients using Sarcophagi
[HarmonyPatch(typeof(Pawn), nameof(Pawn.CurrentlyUsableForBills))]
public static class Harmony_Pawn_CurrentlyUsableForBills
{
    public static void Postfix(ref bool __result, Pawn __instance)
    {
        if (__instance.InBed() 
            && __instance.CurrentBed() is Building_Bed_Sarcophagus bedSarcophagus)
        {
            JobFailReason.Is("RG_Sarcophagus_SurgeryProhibited_PatientUsingSarcophagus".Translate());
            __result = false;
        }
    }
}
