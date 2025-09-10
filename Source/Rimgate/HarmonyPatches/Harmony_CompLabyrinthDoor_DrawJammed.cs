using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(CompLabyrinthDoor), nameof(CompLabyrinthDoor.DrawJammed))]
public static class Patch_CompLabyrinthDoor_DrawJammed
{
    private static bool IsSecretDoor(Thing thing)
    {
        // Resolve once, then cache to avoid string compares every frame.
        return thing?.def == RimgateDefOf.Rimgate_SecretDoor;
    }

    // Return false to skip vanilla; true to let vanilla run.
    static bool Prefix(CompLabyrinthDoor __instance, Rot4 rotation)
    {
        var door = __instance?.parent as Building_JammedDoor;
        if (door == null || !IsSecretDoor(door))
            return true; // not our def → run vanilla

        var gd = __instance.Props?.jammed; // GraphicData for the jammed overlay
        var g = gd?.Graphic;
        if (g == null)
            return true; // nothing to draw → let vanilla try

        // Create a tinted version regardless of XML colorFromStuff.
        // Use both DrawColor and DrawColorTwo to respect stuff dual-tint shaders.
        var tinted = g.GetColoredVersion(g.Shader, door.DrawColor, door.DrawColorTwo);

        var drawPos = door.DrawPos;
        drawPos.y = AltitudeLayer.DoorMoveable.AltitudeFor();

        tinted.Draw(drawPos, rotation, door);
        return false; // we've drawn it; skip vanilla
    }
}
