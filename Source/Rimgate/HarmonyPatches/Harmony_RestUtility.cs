using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimgate.HarmonyPatches;

// Check if patient is (still) allowed to use the Sarcophagus
[HarmonyPatch(typeof(RestUtility), nameof(RestUtility.CanUseBedNow))]
public static class Harmony_RestUtility
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
            && MedicalUtility.HasAllowedMedicalCareCategory(pawn)
            // Pawn type (colonist, slave, prisoner, guest) matches bedtype
            && SarcophagusUtility.IsValidForUserType(bedSarcophagus, pawn)
            // Sarcophagus hasn't been aborted
            && !bedSarcophagus.Aborted;
    }
}

// Exclude Sarcophagus beds as possible prisoner beds when capturing new prisoners
[HarmonyPatch(typeof(RestUtility), nameof(RestUtility.IsValidBedFor))]
public static class Harmony_RestUtility_IsValidBedFor
{
    public static void Postfix(
        ref bool __result,
        Thing bedThing,
        Pawn sleeper,
        GuestStatus? guestStatus = null)
    {
        // !sleeper.IsPrisonerOfColony and GuestStatus.Prisoner indicates
        // that the target sleeper pawn is currently not
        // a prisoner of the player colony (but is about to be!)
        if (__result
            && !sleeper.IsPrisonerOfColony
            && guestStatus == GuestStatus.Prisoner
            && bedThing is Building_Bed_Sarcophagus) __result = false;
    }
}

// Exclude Sarcophagus beds as possible prisoner beds when capturing new prisoners
[HarmonyPatch(typeof(RestUtility), nameof(RestUtility.TuckIntoBed))]
public static class Harmony_RestUtility_TuckIntoBed
{
    public static bool Prefix(
        Building_Bed bed,
        Pawn taker,
        Pawn takee,
        bool rescued)
    {
        if (bed is Building_Bed_Sarcophagus bedSarcophagus)
        {
            SarcophagusUtility.PutIntoSarcophagus(bedSarcophagus, taker, takee, true);

            return false;
        }

        return true;
    }
}
