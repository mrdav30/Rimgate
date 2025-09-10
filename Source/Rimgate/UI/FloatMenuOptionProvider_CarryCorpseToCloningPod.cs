using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class FloatMenuOptionProvider_HandleCorpse : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => false;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        Corpse corpse = clickedThing as Corpse;
        if (corpse == null)
            yield break;

        Building_WraithCloningPod pod = Building_WraithCloningPod.FindCloningPodFor(corpse, context.FirstSelectedPawn);
        if (pod == null)
            yield break;

        Action action = delegate
        {
            Building_WraithCloningPod cloningPod = Building_WraithCloningPod.FindCloningPodFor(corpse, context.FirstSelectedPawn);
            if (cloningPod == null)
            {
                cloningPod = Building_WraithCloningPod.FindCloningPodFor(corpse, context.FirstSelectedPawn, ignoreOtherReservations: true);
            }

            Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_CarryCorpseToCloningPod, corpse, cloningPod);
            job.count = 1;
            context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        };


        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(
                "PlaceIn".Translate(corpse, pod),
                action),
            context.FirstSelectedPawn,
            new LocalTargetInfo(corpse));
    }
}

