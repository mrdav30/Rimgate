using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimgate.HarmonyPatches;

// Check if patient is (still) allowed to use the Sarcophagus
[HarmonyPatch(typeof(RestUtility), nameof(RestUtility.CanUseBedNow))]
public static class Harmony_RestUtility_CanUseBedNow
{
    public static bool Postfix(bool __result, Thing bedThing, Pawn sleeper)
    {
        return (bedThing is Building_Bed_Sarcophagus bedSarcophagus) 
            ? CanUseSarcophagus(bedSarcophagus, sleeper)
            : __result;
    }

    public static bool CanUseSarcophagus(Building_Bed_Sarcophagus bedSarcophagus, Pawn pawn)
    {
        return 
            // Sarcophagus has power
            bedSarcophagus.Power.PowerOn
            && !bedSarcophagus.HasAnyContents
            // Sarcophagus is not forbidden for the pawn
            && !bedSarcophagus.IsForbidden(pawn) 
            // Pawn actually has a medical need for a Sarcophagus
            && SarcophagusUtility.ShouldSeekSarcophagus(pawn, bedSarcophagus)
            // Pawn has medical care category that allows Sarcophagus use
            && HealthUtility.HasAllowedMedicalCareCategory(pawn)
            // Pawn type (colonist, slave, prisoner, guest) matches bedtype
            && SarcophagusUtility.IsValidForUserType(bedSarcophagus, pawn)
            // Sarcophagus hasn't been aborted
            && !bedSarcophagus.Aborted;
    }
}