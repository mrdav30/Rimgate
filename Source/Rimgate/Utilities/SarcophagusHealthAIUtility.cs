using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

public static class SarcophagusHealthAIUtility
{
    public static bool ShouldSeekSarcophagusRest(Pawn patientPawn, Building_Bed_Sarcophagus bedSarcophagus)
    {
        List<Hediff> hediffList = new List<Hediff>();
        patientPawn.health.hediffSet.GetHediffs<Hediff>(ref hediffList, (Predicate<Hediff>)null);
        List<HediffDef> alwaysTreatableHediffs = ((ThingWithComps)bedSarcophagus).GetComp<Comp_TreatmentRestrictions>().Props.alwaysTreatableHediffs;
        List<HediffDef> neverTreatableHediffs = ((ThingWithComps)bedSarcophagus).GetComp<Comp_TreatmentRestrictions>().Props.neverTreatableHediffs;
        List<HediffDef> nonCriticalTreatableHediffs = ((ThingWithComps)bedSarcophagus).GetComp<Comp_TreatmentRestrictions>().Props.nonCriticalTreatableHediffs;
        List<HediffDef> usageBlockingHediffs = ((ThingWithComps)bedSarcophagus).GetComp<Comp_TreatmentRestrictions>().Props.usageBlockingHediffs;
        List<TraitDef> usageBlockingTraits = ((ThingWithComps)bedSarcophagus).GetComp<Comp_TreatmentRestrictions>().Props.usageBlockingTraits;
        bool flag1 = patientPawn.Downed && !LifeStageUtility.AlwaysDowned(patientPawn);
        bool flag2 = GenCollection.Any<Hediff>(hediffList, (Predicate<Hediff>)(x => x.Visible && x.TendableNow(false) && !neverTreatableHediffs.Contains(x.def) && !nonCriticalTreatableHediffs.Contains(x.def)));
        bool flag3 = patientPawn.health.hediffSet.HasTendedAndHealingInjury();
        bool flag4 = patientPawn.health.hediffSet.HasImmunizableNotImmuneHediff();
        bool flag5 = GenCollection.Any<Hediff>(hediffList, (Predicate<Hediff>)(x => x.def.makesSickThought && x.Visible && !neverTreatableHediffs.Contains(x.def) && !nonCriticalTreatableHediffs.Contains(x.def)));
        bool flag6 = !GenList.NullOrEmpty<Hediff_MissingPart>((IList<Hediff_MissingPart>)patientPawn.health.hediffSet.GetMissingPartsCommonAncestors().Where<Hediff_MissingPart>((Func<Hediff_MissingPart, bool>)(x => ((Hediff)x).def.isBad)).ToList<Hediff_MissingPart>()) && !neverTreatableHediffs.Contains(HediffDefOf.MissingBodyPart);
        bool flag7 = GenCollection.Any<Hediff>(hediffList, (Predicate<Hediff>)(x => HediffUtility.IsPermanent(x) && !neverTreatableHediffs.Contains(x.def)));
        bool flag8 = GenCollection.Any<Hediff>(hediffList, (Predicate<Hediff>)(x => x.def.chronic && !neverTreatableHediffs.Contains(x.def) && !nonCriticalTreatableHediffs.Contains(x.def)));
        bool flag9 = GenCollection.Any<Hediff>(hediffList, (Predicate<Hediff>)(x => x.def.IsAddiction && !neverTreatableHediffs.Contains(x.def) && !nonCriticalTreatableHediffs.Contains(x.def)));
        bool flag10 = GenCollection.Any<Hediff>(hediffList, (Predicate<Hediff>)(x => alwaysTreatableHediffs.Contains(x.def)));
        bool flag11 = RestUtility.CurrentBed(patientPawn) == bedSarcophagus && GenCollection.Any<Hediff>(hediffList, (Predicate<Hediff>)(x => nonCriticalTreatableHediffs.Contains(x.def)));
        bool flag12 = !SarcophagusHealthAIUtility.HasUsageBlockingHediffs(patientPawn, usageBlockingHediffs) && !SarcophagusHealthAIUtility.HasUsageBlockingTraits(patientPawn, usageBlockingTraits);
        bool flag13 = (flag1 | flag2 | flag3 | flag4 | flag5 | flag6 | flag7 | flag8 | flag9 | flag10 | flag11) & flag12;

        if (RimgateMod.debug)
            Log.Message($"{$"{patientPawn} should use {bedSarcophagus}? = {GenText.ToStringYesNo(flag13)}\n"}isDowned = {GenText.ToStringYesNo(flag1)}\nhasTendableHediffs = {GenText.ToStringYesNo(flag2)}\nhasTendedAndHealingInjuries = {GenText.ToStringYesNo(flag3)}\nhasImmunizableNotImmuneHediffs = {GenText.ToStringYesNo(flag4)}\nhasSickThoughtHediffs = {GenText.ToStringYesNo(flag5)}\nhasMissingBodyParts = {GenText.ToStringYesNo(flag6)}\nhasPermanentInjuries = {GenText.ToStringYesNo(flag7)}\nhasChronicDiseases = {GenText.ToStringYesNo(flag8)}\nhasAddictions = {GenText.ToStringYesNo(flag9)}\nhasAlwaysTreatableHediffs = {GenText.ToStringYesNo(flag10)}\nhasGreylistedHediffsDuringTreatment = {GenText.ToStringYesNo(flag11)}\nhasNoBlockingHediffsOrTraits = {GenText.ToStringYesNo(flag12)}");

        return flag13;
    }

    public static bool IsValidRaceForSarcophagus(Pawn patientPawn, List<string> disallowedRaces)
    {
        string str = ((Thing)patientPawn).def.ToString();
        return GenList.NullOrEmpty<string>((IList<string>)disallowedRaces) || !disallowedRaces.Contains(str);
    }

    public static bool IsValidXenotypeForSarcophagus(
      Pawn patientPawn,
      List<XenotypeDef> disallowedXenotypes)
    {
        XenotypeDef xenotype = patientPawn.genes?.Xenotype;
        return xenotype == null || GenList.NullOrEmpty<XenotypeDef>((IList<XenotypeDef>)disallowedXenotypes) || !disallowedXenotypes.Contains(xenotype);
    }

    public static bool HasAllowedMedicalCareCategory(Pawn patientPawn)
    {
        return (byte)WorkGiver_DoBill.GetMedicalCareCategory((Thing)patientPawn) >= 3;
    }

    public static bool HasUsageBlockingHediffs(Pawn patientPawn, List<HediffDef> usageBlockingHediffs)
    {
        List<Hediff> hediffList = new List<Hediff>();
        patientPawn.health.hediffSet.GetHediffs<Hediff>(ref hediffList, (Predicate<Hediff>)null);
        return GenCollection.Any<Hediff>(hediffList, (Predicate<Hediff>)(x => usageBlockingHediffs.Contains(x.def)));
    }

    public static bool HasUsageBlockingTraits(Pawn patientPawn, List<TraitDef> usageBlockingTraits)
    {
        Pawn_StoryTracker story = patientPawn.story;
        return story != null && GenCollection.Any<Trait>(story.traits.allTraits, (Predicate<Trait>)(x => usageBlockingTraits.Contains(x.def)));
    }
}
