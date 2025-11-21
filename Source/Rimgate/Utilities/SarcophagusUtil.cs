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

    public static bool IsValidForUserType(
        Building_Sarcophagus s,
        Pawn p)
    {
        if (!p.RaceProps.Humanlike || p.IsMutant && !p.mutant.Def.entitledToMedicalCare)
            return false;

        if (!s.AllowSlaves && (p.IsSlaveOfColony || p.GuestStatus == GuestStatus.Slave))
        {
            JobFailReason.Is("RG_Sarcophagus_SlavesNotAllowed".Translate());
            return false;
        }

        if (!s.AllowPrisoners && (p.IsPrisonerOfColony || p.GuestStatus == GuestStatus.Prisoner))
        {
            JobFailReason.Is("RG_Sarcophagus_PrisonersNotAllowed".Translate());
            return false;
        }

        if (!p.IsColonist && p.GuestStatus == GuestStatus.Guest)
        {
            if (!s.AllowGuests)
            {

                JobFailReason.Is("RG_Sarcophagus_GuestsNotAllowed".Translate());
                return false;
            }
        }
        else
        {
            var assigned = s.GetAssignedPawns();
            if (assigned == null || !assigned.Contains(p))
            {
                JobFailReason.Is("NotAssigned".Translate());
                return false;
            }
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

        if (sarcophagus.Power == null || !sarcophagus.Power.PowerOn)
        {
            JobFailReason.Is("RG_Sarcophagus_Unpowered".Translate());
            return false;
        }

        if (sarcophagus.IsForbidden(traveler))
        {
            JobFailReason.Is("ForbiddenLower".Translate());
            return false;
        }

        if (sarcophagus.IsBurning())
            return false;

        if (sarcophagus.IsBrokenDown())
            return false;

        if (sarcophagus.HasAnyContents)
        {
            var other = sarcophagus.PatientPawn;
            JobFailReason.Is("SomeoneElseSleeping".Translate(other));
            return false;
        }

        if (patient.BodySize > Building_Sarcophagus.MaxBodySize)
        {
            JobFailReason.Is("TooLargeForBed".Translate());
            return false;
        }

        if (!traveler.CanReserve(sarcophagus))
        {
            Pawn otherPawn = traveler.Map.reservationManager.FirstRespectedReserver(sarcophagus, patient);
            if (otherPawn != null)
                JobFailReason.Is("ReservedBy".Translate(otherPawn.LabelShort, otherPawn));
            return false;
        }

        if (!traveler.CanReach(sarcophagus, PathEndMode.OnCell, Danger.Deadly))
        {
            JobFailReason.Is("NoPathTrans".Translate());
            return false;
        }

        if (traveler.Map.designationManager.DesignationOn(sarcophagus, DesignationDefOf.Deconstruct) != null)
            return false;

        if (!IsValidForUserType(sarcophagus, patient))
            return false;

        if (!ShouldSeekTreatment(patient, sarcophagus))
        {
            JobFailReason.Is("NotInjured".Translate());
            return false;
        }

        if (!MedicalUtil.HasAllowedMedicalCareCategory(patient))
        {
            JobFailReason.Is("RG_Sarcophagus_MedicalCareCategoryTooLow".Translate());
            return false;
        }

        if (!Utils.IsValidRaceFor(patient, sarcophagus.DisallowedRaces))
        {
            JobFailReason.Is("RG_Sarcophagus_RaceNotAllowed".Translate(patient.def.label.CapitalizeFirst()));
            return false;
        }

        if (!Utils.IsValidXenotypeFor(patient, sarcophagus.DisallowedXenotypes))
        {
            JobFailReason.Is("RG_Sarcophagus_RaceNotAllowed".Translate(patient.genes?.Xenotype.label.CapitalizeFirst()));
            return false;
        }

        if (MedicalUtil.HasUsageBlockingHediffs(patient, sarcophagus.UsageBlockingHediffs))
        {
            var blocking = patient.health.hediffSet.hediffs.FirstOrDefault(h => sarcophagus.UsageBlockingHediffs.Contains(h.def));
            JobFailReason.Is("RG_Sarcophagus_PatientWithHediffNotAllowed".Translate(blocking?.LabelCap ?? ""));
            return false;
        }

        if (MedicalUtil.HasUsageBlockingTraits(patient, sarcophagus.UsageBlockingTraits))
        {
            var blocking = patient.story?.traits.allTraits.FirstOrDefault(t => sarcophagus.UsageBlockingTraits.Contains(t.def));
            JobFailReason.Is("RG_Sarcophagus_PatientWithTraitNotAllowed".Translate(blocking?.LabelCap ?? ""));
            return false;
        }

        return true;
    }

    public static bool ShouldSeekTreatment(Pawn patient, Building_Sarcophagus sarcophagus)
    {
        var set = patient?.health?.hediffSet;
        if (set == null) return false;

        bool ideoActive = ModsConfig.IdeologyActive && patient.Ideo != null;

        List<Hediff> patientHediffs = new List<Hediff>();
        set.GetHediffs<Hediff>(ref patientHediffs, null);

        CompProperties_TreatmentRestrictions restrictions = sarcophagus.GetComp<Comp_TreatmentRestrictions>().Props;
        List<HediffDef> alwaysTreatableHediffs = restrictions.alwaysTreatableHediffs;
        List<HediffDef> neverTreatableHediffs = restrictions.neverTreatableHediffs;
        List<HediffDef> nonCriticalTreatableHediffs = restrictions.nonCriticalTreatableHediffs;
        List<HediffDef> usageBlockingHediffs = restrictions.usageBlockingHediffs;
        List<TraitDef> usageBlockingTraits = restrictions.usageBlockingTraits;

        // Is downed and not meant to be always downed (e.g. babies)
        bool isDowned = (patient.Downed && !LifeStageUtility.AlwaysDowned(patient));

        // Has (visible) hediffs requiring tending (excluding those blacklisted or greylisted from Sarcophagus treatment)
        bool hasTendableHediffs = patientHediffs.Any(x =>
            x.Visible
            && x.TendableNow()
            && !neverTreatableHediffs.Contains(x.def)
            && !nonCriticalTreatableHediffs.Contains(x.def));

        // Has tended and healing injuries
        bool hasTendedAndHealingInjuries = set.HasTendedAndHealingInjury();

        // Has immunizable but not yet immune hediffs
        bool hasImmunizableNotImmuneHediffs = set.HasImmunizableNotImmuneHediff();

        // Has (visible) hediffs causing sick thoughts (excluding those blacklisted or greylisted from Sarcophagus treatment)
        bool hasSickThoughtHediffs = patientHediffs.Any(x =>
            x.def.makesSickThought
            && x.Visible
            && !neverTreatableHediffs.Contains(x.def)
            && !nonCriticalTreatableHediffs.Contains(x.def));

        bool canRegrowMissingParts = ResearchUtil.SarcophagusBioregenerationComplete;
        bool blindnessRequired = ideoActive ? patient.ideo.Ideo.BlindPawnChance > 0 : false;

        // Has missing body parts (excluding those flagged as not bad + ideological blindness requirement)
        bool hasMissingBodyParts = canRegrowMissingParts
            && !set.GetMissingPartsCommonAncestors()
                .Where(x =>
                    x.def.isBad
                    // If blindness is required, ignore missing sight-source parts (ideological choice)
                    && (!blindnessRequired || !x.Part.def.tags.Contains(BodyPartTagDefOf.SightSource)))
                .ToList()
                .NullOrEmpty()
            && !neverTreatableHediffs.Contains(HediffDefOf.MissingBodyPart);

        // Has permanent injuries (excluding those blacklisted from Sarcophagus treatment)
        bool hasPermanentInjuries = patientHediffs.Any(x =>
            x.IsPermanent()
            && !neverTreatableHediffs.Contains(x.def));

        // Has chronic diseases (excluding those blacklisted or greylisted from Sarcophagus treatment)
        bool hasChronicDiseases = patientHediffs.Any(x =>
            x.def.chronic
            && !neverTreatableHediffs.Contains(x.def)
            && !nonCriticalTreatableHediffs.Contains(x.def));

        // Has addictions (excluding those blacklisted or greylisted from Sarcophagus treatment)
        bool hasAddictions = patientHediffs.Any(x =>
            x.def.IsAddiction
            && !neverTreatableHediffs.Contains(x.def)
            && !nonCriticalTreatableHediffs.Contains(x.def));

        bool hasSarcophagusNeed = false;
        if (patient.needs.TryGetNeed(RimgateDefOf.Rimgate_SarcophagusChemicalNeed, out var need)
            && need.CurLevel <= 0.5) hasSarcophagusNeed = true;

        // Has hediffs that are always treatable by Sarcophaguss
        bool hasAlwaysTreatableHediffs = patientHediffs.Any(x => alwaysTreatableHediffs.Contains(x.def));

        // Is already using a Sarcophagus and has any greylisted hediffs
        bool hasGreylistedHediffsDuringTreatment = patient.ParentHolder == sarcophagus
            && patientHediffs.Any(x => nonCriticalTreatableHediffs.Contains(x.def));

        // Does not have hediffs or traits that block the pawn from using Sarcophaguss
        bool hasNoBlockingHediffsOrTraits = !MedicalUtil.HasUsageBlockingHediffs(patient, usageBlockingHediffs)
            && !MedicalUtil.HasUsageBlockingTraits(patient, usageBlockingTraits);

        bool hasOtherHediffs = isDowned
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

        // Ideology: if this pawn's ideology expects scars, don't send them to sarcophagus
        // *only* for that purpose.
        if (ideoActive)
        {
            //int scarCount = patient.health.hediffSet.GetHediffCount(HediffDefOf.Scarification);
            int requiredScars = patient.ideo.Ideo.RequiredScars;
            if (requiredScars > 0 && !hasOtherHediffs)
            {
                if (RimgateMod.Debug)
                    Log.Message($"{patient} ignoring sarcophagus due to ideology required scars.");
                return false;
            }
        }

        bool result = hasOtherHediffs && hasNoBlockingHediffsOrTraits;

        if (RimgateMod.Debug)
            Log.Message($"{patient} should use {sarcophagus}? = {result.ToStringYesNo()}\n"
                + $"isDowned = {isDowned.ToStringYesNo()}\n"
                + $"hasTendableHediffs = {hasTendableHediffs.ToStringYesNo()}\n"
                + $"hasTendedAndHealingInjuries = {hasTendedAndHealingInjuries.ToStringYesNo()}\n"
                + $"hasImmunizableNotImmuneHediffs = {hasImmunizableNotImmuneHediffs.ToStringYesNo()}\n"
                + $"hasSickThoughtHediffs = {hasSickThoughtHediffs.ToStringYesNo()}\n"
                + $"hasMissingBodyParts = {hasMissingBodyParts.ToStringYesNo()}\n"
                + $"hasPermanentInjuries = {hasPermanentInjuries.ToStringYesNo()}\n"
                + $"hasChronicDiseases = {hasChronicDiseases.ToStringYesNo()}\n"
                + $"hasAddictions = {hasAddictions.ToStringYesNo()}\n"
                + $"hasAlwaysTreatableHediffs = {hasAlwaysTreatableHediffs.ToStringYesNo()}\n"
                + $"hasGreylistedHediffsDuringTreatment = {hasGreylistedHediffsDuringTreatment.ToStringYesNo()}\n"
                + $"hasNoBlockingHediffsOrTraits = {hasNoBlockingHediffsOrTraits.ToStringYesNo()}");

        return result;
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
