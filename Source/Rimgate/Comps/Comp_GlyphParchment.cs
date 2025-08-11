using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace Rimgate;

public class Comp_GlyphParchment : ThingComp
{
    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (selPawn == null)
            yield break;

        bool canReach = selPawn.CanReach(
            parent,
            PathEndMode.Touch,
            Danger.Deadly,
            false,
            false,
            TraverseMode.ByPawn);
        if (!canReach)
            yield break;

        if (Rimgate_DefOf.Rimgate_GlyphDeciphering.IsFinished)
        {
            yield return new FloatMenuOption("RG_DecodeSGSymbols".Translate(), () =>
            {
                Job job = JobMaker.MakeJob(Rimgate_DefOf.Rimgate_DecodeGlyphs, parent);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
        else
            yield return new FloatMenuOption("RG_CannotDecodeSGSymbols".Translate(), null);
    }
}
