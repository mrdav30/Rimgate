using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

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

        if (!SarcophagusUtil.CanTakeToSarcophagus(rescuer, clickedPawn))
            return null;

        if (clickedPawn.mindState.WillJoinColonyIfRescued)
            return null;

        if (!clickedPawn.RaceProps.Humanlike || clickedPawn.IsColonyMech)
            return null;

        if (clickedPawn.Faction != null && clickedPawn.Faction.HostileTo(Faction.OfPlayer))
            return null;

        if (clickedPawn.IsPrisoner && !rescuer.workSettings.WorkIsActive(WorkTypeDefOf.Warden))
            return new FloatMenuOption("CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": " + "NotAssignedToWorkType".Translate(WorkTypeDefOf.Warden), null);

        Pawn_PlayerSettings playerSettings = clickedPawn.playerSettings;
        if (playerSettings != null && playerSettings.medCare == MedicalCareCategory.NoCare)
            return new FloatMenuOption("CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": " + "MedicalCareDisabled".Translate(), null);

        bool flag = rescuer.CanReserveAndReach(
            clickedPawn,
            PathEndMode.OnCell,
            Danger.Deadly,
            ignoreOtherReservations: true);
        if (!flag)
            return new FloatMenuOption("RG_CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": " + "NoPath".Translate(), null);

        // Get the first Sarcophagus on the map
        Building_Sarcophagus sarcophagus = SarcophagusUtil.FindBestSarcophagus(clickedPawn, rescuer);
        if (sarcophagus == null)
        {
            var reason = JobFailReason.HaveReason
                ? $": {JobFailReason.Reason}"
                : string.Empty;
            return new FloatMenuOption("RG_CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": " + reason, null);
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
