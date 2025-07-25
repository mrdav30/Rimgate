using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;

namespace Rimgate;

public class CompProperties_Sarcophagus : CompProperties
{
    public float maxDiagnosisTime = 5f;

    public float maxPerHediffHealingTime = 10f;

    public float diagnosisModePowerConsumption = 8000f;

    public float healingModePowerConsumption = 32000f;

    public CompProperties_Sarcophagus() => compClass = typeof(Comp_Sarcophagus);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string item in base.ConfigErrors(parentDef))
        {
            yield return item;
        }
        if (maxDiagnosisTime > 30f)
        {
            yield return $"{nameof(CompProperties_Sarcophagus)}.{nameof(maxDiagnosisTime)} above allowed maximum; value capped at 30 seconds";
            maxDiagnosisTime = 30f;
        }
        if (maxPerHediffHealingTime > 30f)
        {
            yield return $"{nameof(CompProperties_Sarcophagus)}.{nameof(maxPerHediffHealingTime)} above allowed maximum; value capped at 30 seconds";
            maxPerHediffHealingTime = 30f;
        }
    }

    [DebuggerHidden]
    public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        foreach (StatDrawEntry specialDisplayStat in base.SpecialDisplayStats(req))
            yield return specialDisplayStat;

        yield return new StatDrawEntry(
            StatCategoryDefOf.Building,
            "RG_Sarcophagus_Stat_PowerConsumptionDiagnosis_Label".Translate(),
            diagnosisModePowerConsumption.ToString("F0") + " W", 
            "RG_Sarcophagus_Stat_PowerConsumptionDiagnosis_Desc".Translate(),
            4994);
        yield return new StatDrawEntry(
            StatCategoryDefOf.Building,
            "RG_Sarcophagus_Stat_PowerConsumptionHealing_Label".Translate(),
            healingModePowerConsumption.ToString("F0") + " W", 
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