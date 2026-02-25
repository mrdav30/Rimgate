using HarmonyLib;
using RimWorld;
using System;

namespace Rimgate.HarmonyPatches;

/// <summary>
/// Sets the state for the build ghost resource readout, 
/// which is used to determine whether or not to show the resource readout for the build ghost.
/// </summary>
[HarmonyPatch(typeof(Designator_Build), "DrawPanelReadout")]
public static class Harmony_Designator_Build_DrawPanelReadout
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
