using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;

// Remove any arresting/carrying options for patients already lying on sarcophagi
[HarmonyPatch]
public static class Harmony_FloatMenuOptionProvider_Generic_GetSingleOptionFor_ClickedPawn_SkipForSarcophagi
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(
            typeof(FloatMenuOptionProvider_Arrest),
            "GetSingleOptionFor",
            new Type[] {
                typeof(Pawn),
                typeof(FloatMenuContext)
            });
        yield return AccessTools.Method(
            typeof(FloatMenuOptionProvider_CarryPawn),
            "GetSingleOptionFor",
            new Type[] {
                typeof(Pawn),
                typeof(FloatMenuContext)
            });
    }

    static void Postfix(ref FloatMenuOption __result, Pawn clickedPawn)
    {
        if (clickedPawn != null
            && clickedPawn.CurrentBed() is Building_Bed_Sarcophagus)
        {
            __result = null;
        }
    }
}

// Remove any stripping/tending options for patients already lying on sarcophagi
[HarmonyPatch]
public static class Harmony_FloatMenuOptionProvider_Generic_GetSingleOptionFor_ClickedThing_SkipForSarcophagi
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return (MethodBase)AccessTools.Method(
            typeof(FloatMenuOptionProvider_Strip),
            "GetSingleOptionFor",
            new Type[2]
            {
              typeof (Thing),
              typeof (FloatMenuContext)
            },
            (Type[])null);

        yield return (MethodBase)AccessTools.Method(
            typeof(FloatMenuOptionProvider_DraftedTend),
            "GetSingleOptionFor",
            new Type[2]
            {
              typeof (Thing),
              typeof (FloatMenuContext)
            },
            (Type[])null);
    }

    private static void Postfix(ref FloatMenuOption __result, Thing clickedThing)
    {
        // Extra check to make sure the clicked target is actually a pawn
        if (clickedThing != null 
            && clickedThing is Pawn clickedPawn 
            && clickedPawn.CurrentBed() is Building_Bed_Sarcophagus) __result = null;
    }
}
