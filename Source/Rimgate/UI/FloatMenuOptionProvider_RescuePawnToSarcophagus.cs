using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace Rimgate;

// Adds a "Rescue [pawn] to Sarcophagus" float menu option to pawns
public class FloatMenuOptionProvider_RescuePawnToSarcophagus : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        Pawn rescuer = context?.FirstSelectedPawn;

        if (clickedPawn == null || rescuer == null) return null;

        // Allow victims:
        // - Who are not in a mental state
        // - Who are colonists with Scaria
        // - Who belong to no faction
        // - Who are not hostile to the player
        if (clickedPawn.InMentalState && !clickedPawn.health.hediffSet.HasHediff(HediffDefOf.Scaria))
        {
            return null;
        }

        if (clickedPawn.Faction == null
            || clickedPawn.Faction != Faction.OfPlayer
            || clickedPawn.Faction.HostileTo(Faction.OfPlayer))
        {
            return null;
        }

        // Get the first Sarcophagus on the map
        Building_Sarcophagus sarcophagus = SarcophagusUtility.FindBestSarcophagus(clickedPawn, rescuer);
        if (sarcophagus == null)
        {
            var reason = JobFailReason.HaveReason
                ? $": {JobFailReason.Reason}"
                : string.Empty;
            return new FloatMenuOption("CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + reason, null);
        }

        if (!rescuer.CanReserveAndReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly, ignoreOtherReservations: true))
        {
            return new FloatMenuOption(
                "CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": "
                    + "NoPath".Translate().CapitalizeFirst(),
                null);
        }

        FloatMenuOption floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(
                "RG_Sarcophagus_FloatMenu_Rescue".Translate(clickedPawn.LabelCap, clickedPawn),
                delegate
                {
                    // Assign a Sarcophagus rescue job to the pawn
                    Job job = JobMaker.MakeJob(
                        RimgateDefOf.Rimgate_RescueToSarcophagus,
                        clickedPawn,
                        sarcophagus);
                    job.count = 1;
                    rescuer.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                },
                MenuOptionPriority.RescueOrCapture,
                null,
                clickedPawn),
            rescuer,
            clickedPawn);

        return floatMenuOption;
    }
}
