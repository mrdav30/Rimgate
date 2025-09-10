using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Rimgate;

public static class SarcophagusUtility
{
    private static List<ThingDef> _sarcophagusDefsBestToWorst =>
        _sarcophagusDefsBestToWorstCached ??= RestUtility.AllBedDefBestToWorst
        .Where(x => x.thingClass == typeof(Building_Bed_Sarcophagus)).ToList();

    private static List<ThingDef> _sarcophagusDefsBestToWorstCached;

    private static List<Thing> _tempSarcophagusList = new();

    public static bool IsValidForUserType(Building_Bed_Sarcophagus sarcophagus, Pawn pawn)
    {
        // skip execution early and return true if patient is an animal
        if (pawn.RaceProps.Animal && !sarcophagus.def.building.bed_humanlike)
            return true;

        // Otherwise, check for humanlike patients
        bool isSlave = pawn.GuestStatus == GuestStatus.Slave;
        bool isPrisoner = pawn.GuestStatus == GuestStatus.Prisoner;

        if (sarcophagus.ForSlaves != isSlave)
            return false;

        if (sarcophagus.ForPrisoners != isPrisoner)
            return false;

        if (sarcophagus.ForColonists
            && (!pawn.IsColonist || pawn.GuestStatus == GuestStatus.Guest)
            && !sarcophagus.AllowGuests)
        {
            return false;
        }

        return true;
    }

    public static bool IsValidSarcophagusFor(
        Building_Bed_Sarcophagus sarcophagus,
        Pawn patient,
        Pawn traveler,
        GuestStatus? guestStatus = null)
    {
        if (sarcophagus == null)
            return false;

        if (!sarcophagus.Power.PowerOn)
            return false;

        if (sarcophagus.IsForbidden(traveler))
            return false;

        if (!traveler.CanReserve(sarcophagus))
        {
            Pawn otherPawn = traveler.Map.reservationManager.FirstRespectedReserver(sarcophagus, patient);
            if (otherPawn != null)
            {
                JobFailReason.Is("ReservedBy".Translate(otherPawn.LabelShort, otherPawn));
            }
            return false;
        }

        if (!traveler.CanReach(sarcophagus, PathEndMode.OnCell, Danger.Deadly))
        {
            JobFailReason.Is("NoPathTrans".Translate());
            return false;
        }

        if (traveler.Map.designationManager.DesignationOn(sarcophagus, DesignationDefOf.Deconstruct) != null)
            return false;

        if (!RestUtility.CanUseBedEver(patient, sarcophagus.def))
            return false;

        if (!IsValidForUserType(sarcophagus, patient))
            return false;

        if (!ShouldSeekSarcophagus(patient, sarcophagus))
            return false;

        if (!MedicalUtility.HasAllowedMedicalCareCategory(patient))
            return false;

        if (!IsValidRaceFor(patient, sarcophagus.DisallowedRaces))
            return false;

        if (!IsValidXenotypeFor(patient, sarcophagus.DisallowedXenotypes))
            return false;

        if (MedicalUtility.HasUsageBlockingHediffs(patient, sarcophagus.UsageBlockingHediffs))
            return false;

        if (MedicalUtility.HasUsageBlockingTraits(patient, sarcophagus.UsageBlockingTraits))
            return false;

        if (sarcophagus.Aborted)
            return false;

        if (sarcophagus.IsBurning())
            return false;

        if (sarcophagus.IsBrokenDown())
            return false;

        return true;
    }

    public static bool ShouldSeekSarcophagus(Pawn patient, Building_Bed_Sarcophagus bedSarcophagus)
    {
        List<Hediff> patientHediffs = new List<Hediff>();
        patient.health.hediffSet.GetHediffs<Hediff>(ref patientHediffs, null);
        CompProperties_TreatmentRestrictions restrictions = bedSarcophagus.GetComp<Comp_TreatmentRestrictions>().Props;
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
        bool hasTendedAndHealingInjuries = patient.health.hediffSet.HasTendedAndHealingInjury();

        // Has immunizable but not yet immune hediffs
        bool hasImmunizableNotImmuneHediffs = patient.health.hediffSet.HasImmunizableNotImmuneHediff();

        // Has (visible) hediffs causing sick thoughts (excluding those blacklisted or greylisted from Sarcophagus treatment)
        bool hasSickThoughtHediffs = patientHediffs.Any(x =>
            x.def.makesSickThought
            && x.Visible
            && !neverTreatableHediffs.Contains(x.def)
            && !nonCriticalTreatableHediffs.Contains(x.def));

        // Has missing body parts (excluding those flagged as not bad)
        bool hasMissingBodyParts = !patient.health.hediffSet.GetMissingPartsCommonAncestors()
            .Where(x => x.def.isBad)
            .ToList()
            .NullOrEmpty() && !neverTreatableHediffs.Contains(HediffDefOf.MissingBodyPart);

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
        bool hasAlwaysTreatableHediffs = patientHediffs.Any(x =>
            alwaysTreatableHediffs.Contains(x.def));

        // Is already using a Sarcophagus and has any greylisted hediffs
        bool hasGreylistedHediffsDuringTreatment = patient.CurrentBed() == bedSarcophagus
            && patientHediffs.Any(x => nonCriticalTreatableHediffs.Contains(x.def));

        // Does not have hediffs or traits that block the pawn from using Sarcophaguss
        bool hasNoBlockingHediffsOrTraits = !MedicalUtility.HasUsageBlockingHediffs(patient, usageBlockingHediffs)
            && !MedicalUtility.HasUsageBlockingTraits(patient, usageBlockingTraits);

        bool result = (isDowned
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
            || hasGreylistedHediffsDuringTreatment) && hasNoBlockingHediffsOrTraits;

        if (RimgateMod.Debug)
            Log.Message($"{patient} should use {bedSarcophagus}? = {result.ToStringYesNo()}\n"
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

    public static bool IsValidRaceFor(Pawn patient, List<string> disallowedRaces)
    {
        string race = patient.def.ToString();
        return disallowedRaces.NullOrEmpty() || !disallowedRaces.Contains(race);
    }

    public static bool IsValidXenotypeFor(
      Pawn patient,
      List<XenotypeDef> disallowedXenotypes)
    {
        XenotypeDef xenotype = patient.genes?.Xenotype;
        return xenotype == null
            || disallowedXenotypes.NullOrEmpty()
            || !disallowedXenotypes.Contains(xenotype);
    }

    public static Building_Bed_Sarcophagus FindBestSarcophagus(Pawn pawn, Pawn patient)
    {
        // Skip if there are no sarcophagus bed defs
        if (!_sarcophagusDefsBestToWorst.Any())
            return null;

        Map map = patient.Map;
        ListerThings listerThings = map.listerThings;
        _tempSarcophagusList.Clear();

        // Prioritize searching for usable sarcophagi by distance, followed by sarcophagus type and path danger level
        try
        {
            foreach (ThingDef sarcophagusDef in _sarcophagusDefsBestToWorst)
            {
                // Skip sarcophagus types that the patient can never use
                if (!RestUtility.CanUseBedEver(patient, sarcophagusDef))
                    continue;

                // Check each sarcophagus thing of the current type on the map, and add the ones usable by the current patient to a temporary list
                foreach (Thing sarcophagus in listerThings.ThingsOfDef(sarcophagusDef))
                {
                    if (sarcophagus is Building_Bed_Sarcophagus { Medical: true } bedSarcophagus
                        && !bedSarcophagus.HasAnyContents
                        && IsValidSarcophagusFor(bedSarcophagus, patient, pawn, patient.GuestStatus))
                    {
                        _tempSarcophagusList.Add(bedSarcophagus);
                    }
                }
            }

            if (_tempSarcophagusList.Count == 0)
                return null;

            // Look for the closest reachable sarcophagus from the temporary list, going down by danger level
            for (int i = 0; i < 2; i++)
            {
                Danger maxDanger = i == 0 ? Danger.None : Danger.Deadly;

                Building_Bed_Sarcophagus bedSarcophagus = (Building_Bed_Sarcophagus)GenClosest.ClosestThingReachable(
                    patient.Position,
                    map,
                    ThingRequest.ForUndefined(),
                    PathEndMode.OnCell,
                    TraverseParms.For(pawn),
                    validator: thing => thing.Position.GetDangerFor(patient, map) <= maxDanger,
                    customGlobalSearchSet: _tempSarcophagusList);

                if (bedSarcophagus != null)
                    return bedSarcophagus;
            }
        }
        finally
        {
            // Clean up out temporary list once we're done
            _tempSarcophagusList.Clear();
        }

        // Can't find any valid sarcophagi
        return null;
    }

    public static void PutIntoSarcophagus(
        Building_Bed_Sarcophagus bed,
        Pawn taker,
        Pawn patient,
        bool rescued)
    {
        if (taker != patient)
            bed.TryAcceptPawn(patient);

        if (IsValidSarcophagusFor(bed, patient, taker))
        {
            if (taker != patient && rescued)
                patient.relations.Notify_RescuedBy(taker);

            patient.mindState.Notify_TuckedIntoBed();
        }
        else
            bed.EjectContents();

        if (patient.IsPrisonerOfColony)
        {
            LessonAutoActivator.TeachOpportunity(
                ConceptDefOf.PrisonerTab,
                patient,
                OpportunityType.GoodToKnow);
        }
    }
}
