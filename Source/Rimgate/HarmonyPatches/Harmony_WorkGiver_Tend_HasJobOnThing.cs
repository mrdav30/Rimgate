using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimgate.HarmonyPatches;

// Prevent Doctors/Wardens from tending patients if:
// - The patient is lying on a Sarcophagus
// - The Sarcophagus is powered
[HarmonyPatch(typeof(WorkGiver_Tend), nameof(WorkGiver_Tend.HasJobOnThing))]
public static class WorkGiver_Tend_HasJobOnThing_HasJobOnThing
{
    public static void Postfix(ref bool __result, Thing t)
    {
        Pawn patient = t as Pawn;
        if (patient.CurrentBed() is Building_Bed_Sarcophagus bedSarcophagus 
            && bedSarcophagus.powerComp.PowerOn)
        {
            __result = false;
        }
    }
}
