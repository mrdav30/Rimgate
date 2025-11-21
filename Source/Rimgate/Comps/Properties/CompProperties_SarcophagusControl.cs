using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;

namespace Rimgate;

public class CompProperties_SarcophagusControl : CompProperties
{
    public float maxDiagnosisTime = 5f;
    public float maxPerHediffHealingTime = 10f;

    public float diagnosisModePowerConsumption = 4000f;
    public float healingModePowerConsumption = 16000f;

    public float powerConsumptionReductionFactor = 0.65f;

    public bool applyAddictionHediff; 
    public float addictiveness;
    public float severity = -1f;
    public float existingAddictionSeverityOffset = 0.1f;
    public float needLevelOffset = 1f;

    public CompProperties_SarcophagusControl() => compClass = typeof(Comp_SarcophagusControl);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string item in base.ConfigErrors(parentDef))
            yield return item;

        if (maxDiagnosisTime > 30f)
        {
            yield return $"{nameof(CompProperties_SarcophagusControl)}.{nameof(maxDiagnosisTime)} above allowed maximum; value capped at 30 seconds";
            maxDiagnosisTime = 30f;
        }

        if (maxPerHediffHealingTime > 30f)
        {
            yield return $"{nameof(CompProperties_SarcophagusControl)}.{nameof(maxPerHediffHealingTime)} above allowed maximum; value capped at 30 seconds";
            maxPerHediffHealingTime = 30f;
        }
    }

    [DebuggerHidden]
    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        foreach (StatDrawEntry specialDisplayStat in base.SpecialDisplayStats(req))
            yield return specialDisplayStat;

        // Compute current effective multiplier:
        float multiplier = ResearchUtil.SarcophagusOptimizationComplete
            ? powerConsumptionReductionFactor
            : 1f;

        float diag = diagnosisModePowerConsumption * multiplier;
        float heal = healingModePowerConsumption * multiplier;

        yield return new StatDrawEntry(
            StatCategoryDefOf.Building,
            "RG_Sarcophagus_Stat_PowerConsumptionDiagnosis_Label".Translate(),
            diag.ToString("F0") + " W",
            "RG_Sarcophagus_Stat_PowerConsumptionDiagnosis_Desc".Translate(),
            4994);

        yield return new StatDrawEntry(
            StatCategoryDefOf.Building,
            "RG_Sarcophagus_Stat_PowerConsumptionHealing_Label".Translate(),
            heal.ToString("F0") + " W",
            "RG_Sarcophagus_Stat_PowerConsumptionHealing_Desc".Translate(),
            4993);

        yield return new StatDrawEntry(
            StatCategoryDefOf.Building,
            "RG_Sarcophagus_Stat_DiagnosisTime_Label".Translate(),
            "RG_Sarcophagus_Stat_TimeSeconds".Translate(maxDiagnosisTime), 
            "RG_Sarcophagus_Stat_DiagnosisTime_Desc".Translate(),
            4992);

        yield return new StatDrawEntry(
            StatCategoryDefOf.Building,
            "RG_Sarcophagus_Stat_PerHediffHealingTime_Label".Translate(),
            "RG_Sarcophagus_Stat_TimeSeconds".Translate(maxPerHediffHealingTime), 
            "RG_Sarcophagus_Stat_PerHediffHealingTime_Desc".Translate(),
            4991);
    }
}