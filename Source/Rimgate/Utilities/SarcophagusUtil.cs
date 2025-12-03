using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using static RimWorld.PsychicRitualRoleDef;

namespace Rimgate;

public static class SarcophagusUtil
{
    private static List<Thing> _tmpSarcophagi;

    public static bool IsValidForUserType(Building_Sarcophagus sarcophagus, Pawn patient)
    {
        if (!sarcophagus.AllowSlaves && (patient.IsSlaveOfColony || patient.GuestStatus == GuestStatus.Slave))
        {
            JobFailReason.Is("RG_Sarcophagus_SlavesNotAllowed".Translate());
            return false;
        }

        if (!sarcophagus.AllowPrisoners && (patient.IsPrisonerOfColony || patient.GuestStatus == GuestStatus.Prisoner))
        {
            JobFailReason.Is("RG_Sarcophagus_PrisonersNotAllowed".Translate());
            return false;
        }

        if (!patient.IsColonist && patient.GuestStatus == GuestStatus.Guest)
        {
            if (!sarcophagus.AllowGuests)
            {

                JobFailReason.Is("RG_Sarcophagus_GuestsNotAllowed".Translate());
                return false;
            }
        }

        if (patient.BodySize > Building_Sarcophagus.MaxBodySize)
        {
            JobFailReason.Is("TooLargeForBed".Translate());
            return false;
        }

        if (patient.GuestStatus != GuestStatus.Guest)
        {
            var assigned = sarcophagus.GetAssignedPawns();
            if (assigned == null || !assigned.Contains(patient))
            {
                JobFailReason.Is("NotAssigned".Translate());
                return false;
            }
        }

        if (!patient.IsValidRaceFor(sarcophagus.DisallowedRaces))
        {
            JobFailReason.Is("RG_Sarcophagus_RaceNotAllowed".Translate(patient.def.label.CapitalizeFirst()));
            return false;
        }

        if (!patient.IsValidXenotypeFor(sarcophagus.DisallowedXenotypes))
        {
            JobFailReason.Is("RG_Sarcophagus_RaceNotAllowed".Translate(patient.genes?.Xenotype.label.CapitalizeFirst()));
            return false;
        }

        return true;
    }

    public static bool IsValidSarcophagusFor(
        Building_Sarcophagus sarcophagus,
        Pawn patient,
        Pawn traveler)
    {
        JobFailReason.Clear();

        if (sarcophagus == null)
        {
            JobFailReason.Is("RG_Sarcophagus_CannotRescue_NoSarcophagus".Translate());
            return false;
        }

        if (patient == null
            || !patient.RaceProps.Humanlike
            || patient.IsMutant && !patient.mutant.Def.entitledToMedicalCare)
        {
            JobFailReason.Is("RG_Sarcophagus_NonHumanoidNotAllowed".Translate());
            return false;
        }

        if (!ShouldSeekTreatment(patient, sarcophagus, out string treatmentDenial))
        {
            JobFailReason.Is(treatmentDenial);
            return false;
        }

        if (sarcophagus.Power == null || !sarcophagus.Power.PowerOn)
        {
            JobFailReason.Is("NoPower".Translate());
            return false;
        }

        if (sarcophagus.IsForbidden(traveler))
        {
            JobFailReason.Is("ForbiddenLower".Translate());
            return false;
        }

        if (traveler.Map.designationManager.DesignationOn(sarcophagus, DesignationDefOf.Deconstruct) != null)
        {
            JobFailReason.Is("RG_Sarcophagus_CannotRescue_NoSarcophagus".Translate());
            return false;
        }

        if (sarcophagus.IsBurning())
        {
            JobFailReason.Is("BurningLower".Translate());
            return false;
        }

        if (sarcophagus.IsBrokenDown())
        {
            JobFailReason.Is("BrokenDown".Translate());
            return false;
        }

        if (!IsValidForUserType(sarcophagus, patient))
            return false;

        if (sarcophagus.HasAnyContents)
        {
            var other = sarcophagus.PatientPawn;
            JobFailReason.Is("SomeoneElseSleeping".Translate(other));
            return false;
        }

        if (!traveler.CanReserve(sarcophagus))
        {
            Pawn otherPawn = traveler.Map.reservationManager.FirstRespectedReserver(sarcophagus, patient);
            TaggedString reservedDenial = otherPawn != null
                ? "ReservedBy".Translate(otherPawn.LabelShort, otherPawn)
                : "Reserved".Translate();
            JobFailReason.Is(reservedDenial);
            return false;
        }

        if (!traveler.CanReach(sarcophagus, PathEndMode.OnCell, Danger.Deadly))
        {
            JobFailReason.Is("NoPathTrans".Translate());
            return false;
        }

        return true;
    }

    public static bool ShouldSeekTreatment(
        Pawn patient,
        Building_Sarcophagus sarcophagus,
        out string reason)
    {
        if (!MedicalUtil.HasAllowedMedicalCareCategory(patient))
        {
            reason = "RG_Sarcophagus_MedicalCareCategoryTooLow".Translate();
            return false;
        }

        var set = patient?.health?.hediffSet;
        if (set == null)
        {
            reason = "NotInjured".Translate();
            return false;
        }

        bool ideoActive = ModsConfig.IdeologyActive && patient.Ideo != null;
        bool canRegrowMissingParts = ResearchUtil.SarcophagusBioregenerationComplete;
        bool scarsRequired = ideoActive ? patient.ideo.Ideo.RequiredScars > 0 : false;
        bool blindnessRequired = ideoActive ? patient.ideo.Ideo.BlindPawnChance > 0 : false;

        // Gather hediffs
        List<Hediff> patientHediffs = set.hediffs;

        List<HediffDef> alwaysTreatableHediffs = sarcophagus.AlwaysTreatableHediffs;
        List<HediffDef> neverTreatableHediffs = sarcophagus.NeverTreatableHediffs;
        List<HediffDef> nonCriticalTreatableHediffs = sarcophagus.NonCriticalTreatableHediffs;
        List<HediffDef> usageBlockingHediffs = sarcophagus.UsageBlockingHediffs;
        List<TraitDef> usageBlockingTraits = sarcophagus.UsageBlockingTraits;

        // Is downed and not meant to be always downed (e.g. babies)
        bool isDowned = (patient.Downed && !LifeStageUtility.AlwaysDowned(patient));

        // Visible tendable hediffs (not black/greylisted)
        bool hasTendableHediffs = patientHediffs.Any(x =>
            x.Visible
            && x.TendableNow()
            && !neverTreatableHediffs.Contains(x.def)
            && !nonCriticalTreatableHediffs.Contains(x.def));

        // Tended + healing injuries
        bool hasTendedAndHealingInjuries = set.HasTendedAndHealingInjury();

        // Has immunizable but not yet immune hediffs
        bool hasImmunizableNotImmuneHediffs = set.HasImmunizableNotImmuneHediff();

        // Sick-thought-causing hediffs (not black/greylisted)
        bool hasSickThoughtHediffs = patientHediffs.Any(x =>
            x.def.makesSickThought
            && x.Visible
            && !neverTreatableHediffs.Contains(x.def)
            && !nonCriticalTreatableHediffs.Contains(x.def));

        // Missing body parts (if regrowth is unlocked)
        bool hasMissingBodyParts = canRegrowMissingParts
            && !set.GetMissingPartsCommonAncestors()
                .Where(x =>
                    x.def.isBad
                    // If blindness is required, ignore missing sight-source parts
                    && (!blindnessRequired || !x.Part.def.tags.Contains(BodyPartTagDefOf.SightSource)))
                .ToList()
                .NullOrEmpty()
            && !neverTreatableHediffs.Contains(HediffDefOf.MissingBodyPart);

        // Permanent injuries (but skip ideology scars if needed)
        bool hasPermanentInjuries = patientHediffs.Any(x =>
            x.IsPermanent()
            && (!scarsRequired || x.def != HediffDefOf.Scarification)
            && !neverTreatableHediffs.Contains(x.def));

        // Chronic diseases (but skip Blindness if ideology wants blindness)
        bool hasChronicDiseases = patientHediffs.Any(x =>
            x.def.chronic
            && (!blindnessRequired || x.def != HediffDefOf.Blindness)
            && !neverTreatableHediffs.Contains(x.def)
            && !nonCriticalTreatableHediffs.Contains(x.def));

        // Addictions (not black/greylisted)
        bool hasAddictions = patientHediffs.Any(x =>
            x.def.IsAddiction
            && !neverTreatableHediffs.Contains(x.def)
            && !nonCriticalTreatableHediffs.Contains(x.def));

        // Sarcophagus chemical need
        bool hasSarcophagusNeed = false;
        if (patient.needs.TryGetNeed(RimgateDefOf.Rimgate_SarcophagusChemicalNeed, out var need)
            && need.CurLevel <= 0.5)
        {
            hasSarcophagusNeed = true;
        }

        // Always-treatable hediffs
        bool hasAlwaysTreatableHediffs = patientHediffs.Any(x => alwaysTreatableHediffs.Contains(x.def));

        // Greylisted hediffs if already in sarcophagus
        bool hasGreylistedHediffsDuringTreatment = patient.ParentHolder == sarcophagus
            && patientHediffs.Any(x => nonCriticalTreatableHediffs.Contains(x.def));

        // Any “real” reason to use the sarcophagus at all
        bool needsTreatment = isDowned
            || hasTendableHediffs
            || hasTendedAndHealingInjuries
            || hasImmunizableNotImmuneHediffs
            || hasSickThoughtHediffs
            || hasMissingBodyParts
            || hasPermanentInjuries
            || hasChronicDiseases
            || hasAddictions
            || hasSarcophagusNeed
            || hasAlwaysTreatableHediffs
            || hasGreylistedHediffsDuringTreatment;

        // Nothing but ideology scars or blindness?
        // Already handled implicitly by skipping them in the relevant checks above:
        // - scarsRequired => Scarification never contributes to hasPermanentInjuries.
        // - blindnessRequired => Blindness never contributes to hasChronicDiseases.
        // So if those are the *only* issues, needsTreatment will be false.

        if (RimgateMod.Debug)
            Log.Message($"{patient} should use {sarcophagus}? = {needsTreatment.ToStringYesNo()}");

        if (!needsTreatment)
        {
            reason = "NotInjured".Translate();
            return false;
        }

        // Does not have hediffs or traits that block the pawn from using Sarcophagi
        if (MedicalUtil.HasUsageBlockingHediffs(
            patient,
            usageBlockingHediffs,
            out List<Hediff> blockingHediffs))
        {
            reason = "RG_Sarcophagus_PatientWithHediffNotAllowed".Translate(blockingHediffs[0]?.LabelCap ?? "");
            return false;
        }

        if (MedicalUtil.HasUsageBlockingTraits(
            patient,
            usageBlockingTraits,
            out List<Trait> blockingTraits))
        {
            reason = "RG_Sarcophagus_PatientWithTraitNotAllowed".Translate(blockingTraits[0]?.LabelCap ?? "");
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public static Building_Sarcophagus FindBestSarcophagus(Pawn patient, Pawn traveler)
    {
        Map map = traveler.Map;
        ListerThings listerThings = map?.listerThings;
        if (listerThings == null) return null;

        _tmpSarcophagi ??= new List<Thing>();
        _tmpSarcophagi.Clear();

        // Prioritize searching for usable sarcophagi by distance,
        // followed by sarcophagus type and path danger level
        foreach (Thing thing in listerThings.AllThings)
        {
            if (thing is not Building_Sarcophagus sarcophagus) continue;

            if (sarcophagus.Faction != traveler.Faction) continue;

            // Check each sarcophagus thing of the current type on the map,
            // and add the ones usable by the current patient to a temporary list
            if (IsValidSarcophagusFor(sarcophagus, patient, traveler))
                _tmpSarcophagi.Add(sarcophagus);
        }

        if (_tmpSarcophagi.Count < 0)
            return null;

        // Look for the closest reachable sarcophagus from the temporary list,
        // going down by danger level
        for (int i = 0; i < 2; i++)
        {
            Danger maxDanger = i == 0 ? Danger.None : Danger.Deadly;

            Building_Sarcophagus sarcophagus = GenClosest.ClosestThingReachable(
                traveler.Position,
                map,
                ThingRequest.ForUndefined(),
                PathEndMode.OnCell,
                TraverseParms.For(traveler),
                validator: thing => thing.Position.GetDangerFor(traveler, map) <= maxDanger,
                customGlobalSearchSet: _tmpSarcophagi) as Building_Sarcophagus;

            if (sarcophagus != null)
                return sarcophagus;
        }

        // nothing found
        return null;
    }

    public static bool CanTakeToSarcophagus(Pawn rescuer, Pawn patient)
    {
        if (patient.IsDeactivated())
            return false;

        if (CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(patient))
            return false;

        if (patient.IsMutant && !patient.mutant.Def.entitledToMedicalCare)
            return false;

        if (patient.InMentalState && !patient.health.hediffSet.HasHediff(HediffDefOf.Scaria))
            return false;

        if (patient.ShouldBeSlaughtered())
            return false;

        if (patient.TryGetLord(out var lord)
            && lord.LordJob is LordJob_Ritual lordJob_Ritual
            && lordJob_Ritual.TryGetRoleFor(patient, out var role) && role.allowDowned)
        {
            return false;
        }

        if (LifeStageUtility.AlwaysDowned(patient) && patient.health.hediffSet.InLabor())
            return false;

        return true;
    }

    public static void PutIntoSarcophagus(
        Building_Sarcophagus sarcophagus,
        Pawn traveler,
        Pawn patient,
        bool rescued)
    {
        if (IsValidSarcophagusFor(sarcophagus, patient, traveler))
        {
            if (traveler != patient)
            {
                sarcophagus.TryAcceptPawn(patient);
                if (rescued)
                    patient.relations.Notify_RescuedBy(traveler);
            }
        }
        else
        {
            var reason = JobFailReason.HaveReason
                ? $": {JobFailReason.Reason}"
                : string.Empty;
            Messages.Message(
                "RG_CannotRescuePawn".Translate(patient.Named("PAWN")) + reason,
                traveler,
                MessageTypeDefOf.NeutralEvent);
        }

        if (patient.IsPrisonerOfColony)
            LessonAutoActivator.TeachOpportunity(
                ConceptDefOf.PrisonerTab,
                patient,
                OpportunityType.GoodToKnow);
    }
}
