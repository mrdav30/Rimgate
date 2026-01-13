using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class FloatMenuOptionProvider_CarryToCloningPod : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    public override bool TargetThingValid(Thing thing, FloatMenuContext context)
    {
        return thing is Pawn pawn
            && !pawn.Downed 
            && pawn.RaceProps.IsFlesh;
    }

    public virtual IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (!clickedPawn.Downed || !clickedPawn.RaceProps.IsFlesh)
            yield break;

        var selected = context.FirstSelectedPawn;

        if (!selected.CanReserveAndReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
            yield break;

        if (!ResearchUtil.WraithCloneGenomeComplete)
            yield break;

        Building_CloningPod cloningPod = Building_CloningPod.FindCloningPodFor(clickedPawn, selected);
        if (cloningPod == null)
            yield break;

        TaggedString taggedString = "PlaceIn".Translate(clickedPawn, cloningPod);
        if (clickedPawn.IsQuestLodger())
        {
            var label = "CryptosleepCasketGuestsNotAllowed".Translate();
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    $"{taggedString} ({label})",
                    null,
                    MenuOptionPriority.Default,
                    null,
                    clickedPawn),
                selected,
                clickedPawn);

            yield break;
        }

        if (clickedPawn.GetExtraHostFaction() != null)
        {
            string label = "CryptosleepCasketGuestPrisonersNotAllowed".Translate();
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    $"{taggedString} ({label})",
                    null,
                    MenuOptionPriority.Default,
                    null,
                    clickedPawn),
                context.FirstSelectedPawn,
                clickedPawn);

            yield break;
        }

        yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    $"{taggedString} ({"RG_BeginCloneGenome".Translate()})",
                    () => AssignJob(CloneType.Genome),
                    revalidateClickTarget: clickedPawn),
                selected,
                clickedPawn);

        if (ResearchUtil.WraithCloneFullComplete)
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    $"{taggedString} ({"RG_BeginCloneFull".Translate()})",
                    () => AssignJob(CloneType.Full),
                    revalidateClickTarget: clickedPawn),
                selected,
                clickedPawn);

        if (ResearchUtil.WraithCloneEnhancementComplete)
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    $"{taggedString} ({"RG_BeginCloneSoldier".Translate()})",
                    () => AssignJob(CloneType.Enhanced),
                    revalidateClickTarget: clickedPawn),
                selected,
                clickedPawn);

        void AssignJob(CloneType jobType)
        {
            Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_CarryToCloningPod, clickedPawn, cloningPod);
            job.count = 1;
            if (selected.jobs.TryTakeOrderedJob(job, JobTag.Misc, false))
                cloningPod.SetCloningType(jobType);
        }
    }
}
