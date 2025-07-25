using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate.HarmonyPatches;

// Humanoid patients should always lie on their backs when using Sarcophagi,
// while non-humanoid (animal) pawns should always lie on their sides
[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.LayingFacing))]
public static class Harmony_PawnRenderer_LayingFacing
{
    public static void Postfix(ref Rot4 __result, Pawn ___pawn)
    {
        if (___pawn.CurrentBed() is Building_Bed_Sarcophagus bedSarcophagus)
        {
            if (___pawn.RaceProps.Humanlike)
                __result = Rot4.South;
            else
                __result = bedSarcophagus.Rotation == Rot4.West 
                    ? Rot4.East 
                    : Rot4.West;
        }
    }
}