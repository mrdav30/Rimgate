using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace Rimgate;

public class FloatMenuOptionProvider_CarryToCloningPod : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (!clickedPawn.Downed)
            return null;

        if (!context.FirstSelectedPawn.CanReserveAndReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
        {
            return null;
        }

        if (Building_WraithCloningPod.FindCloningPodFor(clickedPawn, context.FirstSelectedPawn, ignoreOtherReservations: true) == null)
        {
            return null;
        }

        TaggedString taggedString = "RG_CarryToCloningPod".Translate(clickedPawn.LabelCap, clickedPawn);
        if (clickedPawn.IsQuestLodger())
        {
            var label = "CryptosleepCasketGuestsNotAllowed".Translate();
            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    $"{taggedString} ({label})",
                    null,
                    MenuOptionPriority.Default,
                    null,
                    clickedPawn),
                context.FirstSelectedPawn,
                clickedPawn);
        }

        if (clickedPawn.GetExtraHostFaction() != null)
        {
            string label = "CryptosleepCasketGuestPrisonersNotAllowed".Translate();
            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    $"{taggedString} ({label})",
                    null,
                    MenuOptionPriority.Default,
                    null,
                    clickedPawn),
                context.FirstSelectedPawn,
                clickedPawn);
        }

        Action action = delegate
        {
            Building_WraithCloningPod cloningPod = Building_WraithCloningPod.FindCloningPodFor(clickedPawn, context.FirstSelectedPawn);
            if (cloningPod == null)
            {
                cloningPod = Building_WraithCloningPod.FindCloningPodFor(clickedPawn, context.FirstSelectedPawn, ignoreOtherReservations: true);
            }

            if (cloningPod == null)
            {
                Messages.Message(
                    "RG_NoCloningPod".Translate() + ": " + "RG_CannotCarryToCloningPod".Translate(),
                    clickedPawn,
                    MessageTypeDefOf.RejectInput,
                    historical: false);
            }
            else
            {
                Job job = JobMaker.MakeJob(Rimgate_DefOf.Rimgate_CarryToCloningPod, clickedPawn, cloningPod);
                job.count = 1;
                context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        };

        return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(
                taggedString,
                action,
                MenuOptionPriority.Default,
                null,
                clickedPawn),
            context.FirstSelectedPawn,
            clickedPawn);
    }
}
