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

        bool canReach = selPawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly);
        if (!canReach)
            yield break;

        if (!RimgateDefOf.Rimgate_GlyphDeciphering.IsFinished)
        {
            yield return new FloatMenuOption("RG_CannotDecodeSGSymbols".Translate(), null);
            yield break;
        }

        if (StargateUtility.HasActiveStargateQuest())
        {
            yield return new FloatMenuOption("RG_CannotDecode_SGQuestActive".Translate(), null);
            yield break;
        }

        yield return new FloatMenuOption("RG_DecodeSGSymbols".Translate(), () =>
        {
            Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_DecodeGlyphs, parent);
            selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        });


    }
}
