using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace Rimgate;

internal static class Utils
{
    internal static bool HasActiveGene(this Pawn pawn, GeneDef geneDef)
    {
        if (geneDef is null) return false;
        if (pawn.genes is null) return false;
        return pawn.genes.GetGene(geneDef)?.Active ?? false;
    }

    internal static Hediff ApplyHediff(
        Pawn targetPawn,
        HediffDef hediffDef,
        BodyPartRecord bodyPart,
        int duration, float severity)
    {
        Hediff hediff = HediffMaker.MakeHediff(hediffDef, targetPawn, bodyPart);

        if (severity > float.Epsilon)
        {
            hediff.Severity = severity;
        }

        if (hediff is HediffWithComps hediffWithComps)
        {
            foreach (HediffComp comp in hediffWithComps.comps)
            {
                if (duration > 0 && comp is HediffComp_Disappears hediffComp_Disappears)
                {
                    hediffComp_Disappears.ticksToDisappear = duration;
                }
            }
        }

        targetPawn.health.AddHediff(hediff);
        return targetPawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
    }

    internal static void ThrowDebugText(string text, Vector3 drawPos, Map map)
    {
        MoteMaker.ThrowText(drawPos, map, text, -1f);
    }

    internal static void ThrowDebugText(string text, IntVec3 c, Map map)
    {
        MoteMaker.ThrowText(c.ToVector3Shifted(), map, text, -1f);
    }
}
