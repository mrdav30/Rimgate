using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

public static class SarcophagusHealthAIUtility
{
    public static bool ShouldSeekSarcophagusRest(Pawn patient, Building_Bed_Sarcophagus bedSarcophagus)
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

        // Has (visible) hediffs requiring tending (excluding those blacklisted or greylisted from MedPod treatment)
        bool hasTendableHediffs = patientHediffs.Any(x =>
            x.Visible
            && x.TendableNow()
            && !neverTreatableHediffs.Contains(x.def)
            && !nonCriticalTreatableHediffs.Contains(x.def));

        // Has tended and healing injuries
        bool hasTendedAndHealingInjuries = patient.health.hediffSet.HasTendedAndHealingInjury();

        // Has immunizable but not yet immune hediffs
        bool hasImmunizableNotImmuneHediffs = patient.health.hediffSet.HasImmunizableNotImmuneHediff();

        // Has (visible) hediffs causing sick thoughts (excluding those blacklisted or greylisted from MedPod treatment)
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

        // Has permanent injuries (excluding those blacklisted from MedPod treatment)
        bool hasPermanentInjuries = patientHediffs.Any(x =>
            x.IsPermanent()
            && !neverTreatableHediffs.Contains(x.def));

        // Has chronic diseases (excluding those blacklisted or greylisted from MedPod treatment)
        bool hasChronicDiseases = patientHediffs.Any(x =>
            x.def.chronic
            && !neverTreatableHediffs.Contains(x.def)
            && !nonCriticalTreatableHediffs.Contains(x.def));

        // Has addictions (excluding those blacklisted or greylisted from MedPod treatment)
        bool hasAddictions = patientHediffs.Any(x =>
            x.def.IsAddiction
            && !neverTreatableHediffs.Contains(x.def)
            && !nonCriticalTreatableHediffs.Contains(x.def));

        bool hasSarcophagusNeed = false;
        if (patient.needs.TryGetNeed(Rimgate_DefOf.Rimgate_SarcophagusChemicalNeed, out var need)
            && need.CurLevel <= 0.5) hasSarcophagusNeed = true;

        // Has hediffs that are always treatable by MedPods
        bool hasAlwaysTreatableHediffs = patientHediffs.Any(x =>
            alwaysTreatableHediffs.Contains(x.def));

        // Is already using a MedPod and has any greylisted hediffs
        bool hasGreylistedHediffsDuringTreatment = patient.CurrentBed() == bedSarcophagus
            && patientHediffs.Any(x => nonCriticalTreatableHediffs.Contains(x.def));

        // Does not have hediffs or traits that block the pawn from using MedPods
        bool hasNoBlockingHediffsOrTraits = !HasUsageBlockingHediffs(patient, usageBlockingHediffs)
            && !HasUsageBlockingTraits(patient, usageBlockingTraits);

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

        if (RimgateMod.debug)
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

    public static bool IsValidRaceForSarcophagus(Pawn patient, List<string> disallowedRaces)
    {
        string race = patient.def.ToString();
        return disallowedRaces.NullOrEmpty() || !disallowedRaces.Contains(race);
    }

    public static bool IsValidXenotypeForSarcophagus(
      Pawn patientPawn,
      List<XenotypeDef> disallowedXenotypes)
    {
        XenotypeDef xenotype = patientPawn.genes?.Xenotype;
        return xenotype == null 
            || disallowedXenotypes.NullOrEmpty() 
            || !disallowedXenotypes.Contains(xenotype);
    }

    public static bool HasAllowedMedicalCareCategory(Pawn patientPawn)
    {
        return WorkGiver_DoBill.GetMedicalCareCategory(patientPawn) >= MedicalCareCategory.NormalOrWorse;
    }

    public static bool HasUsageBlockingHediffs(Pawn patientPawn, List<HediffDef> usageBlockingHediffs)
    {
        List<Hediff> patientHediffs = new();
        patientPawn.health.hediffSet.GetHediffs(ref patientHediffs);

        return patientHediffs.Any(x => usageBlockingHediffs.Contains(x.def));
    }

    public static bool HasUsageBlockingTraits(Pawn patientPawn, List<TraitDef> usageBlockingTraits)
    {
        return patientPawn.story?.traits.allTraits.Any(x => usageBlockingTraits.Contains(x.def)) ?? false;
    }
}
