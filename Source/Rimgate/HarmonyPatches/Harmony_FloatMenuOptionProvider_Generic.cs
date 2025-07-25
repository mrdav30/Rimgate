using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch]
public static class Harmony_FloatMenuOptionProvider_Generic
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
        if (!(clickedThing is Pawn pawn) 
            || !(RestUtility.CurrentBed(pawn) is Building_Bed_Sarcophagus)) return;

        __result = (FloatMenuOption)null;
    }
}
