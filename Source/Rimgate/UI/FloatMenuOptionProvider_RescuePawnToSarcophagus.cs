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

        // Only if there is at least one player-owned Sarcophagus on the pawn's current map
        bool haveSarcophagus = rescuer.Map.listerBuildings.ColonistsHaveBuilding((Thing building) =>
            building is Building_Bed_Sarcophagus);
        if (!haveSarcophagus)
            return null;

        // Get the first Sarcophagus on the map
        Building_Bed_Sarcophagus bedSarcophagus = rescuer.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed_Sarcophagus>().First();

        // Skip victims:
        // - Who are unreachable
        // - Who do not need Sarcophagus treatment
        // - Who are already in beds
        // - Who will automatically join the player when rescued
        // - Who have hediffs or traits that prevent them from using Sarcophagi
        // - Who are of races that can't use Sarcophagi
        // - Who are of xenotypes that can't use Sarcophagi
        if (!rescuer.CanReserveAndReach(
            clickedPawn,
            PathEndMode.OnCell,
            Danger.Deadly,
            1,
            -1,
            null,
            ignoreOtherReservations: true))
        {
            return new FloatMenuOption(
                "CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": "
                    + "NoPath".Translate().CapitalizeFirst(),
                null);
        }

        if (!SarcophagusHealthAIUtility.ShouldSeekSarcophagusRest(clickedPawn, bedSarcophagus)
            || clickedPawn.InBed()
            || clickedPawn.mindState.WillJoinColonyIfRescued)
        {
            return null;
        }

        if (SarcophagusHealthAIUtility.HasUsageBlockingHediffs(clickedPawn, bedSarcophagus.UsageBlockingHediffs))
        {
            List<Hediff> blockedHediffs = new();
            clickedPawn.health.hediffSet.GetHediffs(ref blockedHediffs);

            return new FloatMenuOption(
                "CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": "
                + "RG_Sarcophagus_FloatMenu_PatientWithHediffNotAllowed".Translate(
                    blockedHediffs.First(h =>
                        bedSarcophagus.UsageBlockingHediffs.Contains(h.def)).LabelCap),
                null);
        }

        if (SarcophagusHealthAIUtility.HasUsageBlockingTraits(clickedPawn, bedSarcophagus.UsageBlockingTraits))
        {
            return new FloatMenuOption(
                "CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": "
                + "RG_Sarcophagus_FloatMenu_PatientWithTraitNotAllowed".Translate(
                    clickedPawn.story?.traits.allTraits.First(t =>
                        bedSarcophagus.UsageBlockingTraits.Contains(t.def)).LabelCap) + ")",
                null);
        }

        if (!SarcophagusHealthAIUtility.IsValidRaceForSarcophagus(clickedPawn, bedSarcophagus.DisallowedRaces))
        {
            return new FloatMenuOption(
                "CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": "
                + "RG_Sarcophagus_FloatMenu_RaceNotAllowed".Translate(clickedPawn.def.label.CapitalizeFirst()) + ")",
                null);
        }

        if (!SarcophagusHealthAIUtility.IsValidXenotypeForSarcophagus(clickedPawn, bedSarcophagus.DisallowedXenotypes))
        {
            return new FloatMenuOption(
                "CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": "
                + "RG_Sarcophagus_FloatMenu_RaceNotAllowed".Translate(
                    clickedPawn.genes?.Xenotype.label.CapitalizeFirst()) + ")",
                null);
        }

        // Allow victims:
        // - Who are not in a mental state
        // - Who are colonists with Scaria
        // - Who belong to no faction
        // - Who are not hostile to the player
        if ((!clickedPawn.InMentalState || clickedPawn.health.hediffSet.HasHediff(HediffDefOf.Scaria))
            && (clickedPawn.Faction == Faction.OfPlayer
                || clickedPawn.Faction == null
                || !clickedPawn.Faction.HostileTo(Faction.OfPlayer)))
        {
            FloatMenuOption floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    "RG_Sarcophagus_FloatMenu_Rescue".Translate(clickedPawn.LabelCap, clickedPawn),
                    delegate
                    {
                        Building_Bed_Sarcophagus bedSarcophagus = SarcophagusRestUtility.FindBestSarcophagus(rescuer, clickedPawn);

                        // Display message on top screen if no valid Sarcophagus is found
                        if (bedSarcophagus == null)
                        {
                            string pawnType = clickedPawn.IsSlave
                                ? "Slave".Translate()
                                    : clickedPawn.IsPrisoner
                                        ? "PrisonerLower".Translate()
                                        : "Colonist".Translate();

                            string reason = !clickedPawn.RaceProps.Animal
                                ? ((string)"RG_Sarcophagus_Message_CannotRescue_NoSarcophagus".Translate(pawnType.ToLower()))
                                : ((string)"RG_Sarcophagus_Message_CannotRescue_NoVetPod".Translate());
                            Messages.Message("CannotRescue".Translate() + ": " + reason,
                                clickedPawn,
                                MessageTypeDefOf.RejectInput,
                                historical: false);
                        }
                        else
                        {
                            // Assign a Sarcophagus rescue job to the pawn
                            Job job = JobMaker.MakeJob(
                                Rimgate_DefOf.Rimgate_RescueToSarcophagus,
                                clickedPawn,
                                bedSarcophagus);
                            job.count = 1;
                            rescuer.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                        }
                    },
                    MenuOptionPriority.RescueOrCapture,
                    null,
                    clickedPawn),
                rescuer,
                clickedPawn);

            return floatMenuOption;
        }

        // fallback
        return null;
    }
}
