using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class FloatMenuOptionProvider_CarryCorpseToCloningPod : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => false;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    public override bool TargetThingValid(Thing thing, FloatMenuContext context)
    {
        return thing is Corpse corpse
            && corpse.InnerPawn != null
            && corpse.InnerPawn.RaceProps.IsFlesh;
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        Corpse corpse = clickedThing as Corpse;
        if (corpse == null || corpse.InnerPawn == null || !corpse.InnerPawn.RaceProps.IsFlesh)
            yield break;

        var selected = context.FirstSelectedPawn;

        if (!selected.CanReserveAndReach(corpse, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
            yield break;

        if (!ResearchUtil.WraithCloneCorpseComplete)
            yield break;

        Building_CloningPod cloningPod = Building_CloningPod.FindCloningPodFor(corpse, selected);
        if (cloningPod == null)
            yield break;

        Action action = delegate
        {
            Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_CarryCorpseToCloningPod, corpse, cloningPod);
            job.count = 1;
            if (selected.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                cloningPod.SetCloningType(CloneType.Reconstruct);
        };

        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(
                $"{"PlaceIn".Translate(corpse, cloningPod)} ({"RG_BeginCloneSoldier".Translate()})",
                action),
            selected,
            new LocalTargetInfo(corpse));
    }
}

