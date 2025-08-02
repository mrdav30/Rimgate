using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace Rimgate.HarmonyPatches;

[HarmonyPatch(typeof(JobMaker), nameof(JobMaker.MakeJob), new Type[] {
    typeof(JobDef),
    typeof(LocalTargetInfo),
    typeof(LocalTargetInfo)})]
public static class Harmony_JobMaker_MakeJob_Rescue
{
    static void Postfix(
        ref Job __result,
        JobDef def,
        LocalTargetInfo targetA,
        LocalTargetInfo targetB)
    {
        if (def == JobDefOf.Rescue
            && targetB.Thing is Building_Bed_Sarcophagus sarcophagus)
        {
            __result.def = Rimgate_DefOf.Rimgate_RescueToSarcophagus;
        } 
    }
}

[HarmonyPatch(typeof(JobMaker), nameof(JobMaker.MakeJob), new Type[] {
    typeof(JobDef),
    typeof(LocalTargetInfo)})]
public static class Harmony_JobMaker_MakeJob_Flick
{
    static void Postfix(
        ref Job __result,
        JobDef def,
        LocalTargetInfo targetA)
    {
        if (def == JobDefOf.Flick
            && targetA.Thing.HasComp<Comp_Toggle>())
        {
            __result.def = Rimgate_DefOf.Rimgate_Toggle;
        }
    }
}