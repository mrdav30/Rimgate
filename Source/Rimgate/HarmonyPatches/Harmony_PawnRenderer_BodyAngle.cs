using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate.HarmonyPatches;

// Non-humanoid (animal) pawns lying in Sarcophagi shouldn't be offset at a random angle
[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.BodyAngle))]
public static class Harmony_PawnRenderer_BodyAngle
{
    public static void Postfix(ref float __result, Pawn ___pawn)
    {
        if (___pawn.CurrentBed() is Building_Bed_Sarcophagus bedSarcophagus 
            && !___pawn.RaceProps.Humanlike 
            && !bedSarcophagus.def.building.bed_humanlike)
        {
            Rot4 rotation = bedSarcophagus.Rotation;
            if (rotation == Rot4.North)
            {
                __result = -90f;
                return;
            }

            if (rotation == Rot4.South)
            {
                __result = 90f;
                return;
            }

            __result = 0f;
        }
    }
}
