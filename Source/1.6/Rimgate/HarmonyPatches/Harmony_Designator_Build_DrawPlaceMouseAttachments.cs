using HarmonyLib;
using RimWorld;
using System;

namespace Rimgate.HarmonyPatches;

/// <summary>
/// Sets the state for the build ghost resource readout, 
/// which is used to determine whether or not to show the resource readout for the build ghost
/// </summary>
[HarmonyPatch(typeof(Designator_Build), "DrawPlaceMouseAttachments")]
public static class Harmony_Designator_Build_DrawPlaceMouseAttachments
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        MobileContainerHarmonyState.EnterBuildGhostResourceReadout();
    }

    [HarmonyFinalizer]
    public static Exception Finalizer(Exception __exception)
    {
        MobileContainerHarmonyState.ExitBuildGhostResourceReadout();
        return __exception;
    }
}
