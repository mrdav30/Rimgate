using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate.HarmonyPatches;

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
        if (__result && !sleeper.IsPrisonerOfColony 
            && guestStatus == GuestStatus.Prisoner 
            && bedThing is Building_Bed_Sarcophagus)
        {
            __result = false;
        }
    }
}
