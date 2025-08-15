using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

public static class MedicalUtility
{
    public static bool HasAllowedMedicalCareCategory(Pawn pawn)
    {
        return WorkGiver_DoBill.GetMedicalCareCategory(pawn) >= MedicalCareCategory.NormalOrWorse;
    }

    public static bool HasUsageBlockingHediffs(Pawn pawn, List<HediffDef> usageBlockingHediffs)
    {
        List<Hediff> patientHediffs = new();
        pawn.health.hediffSet.GetHediffs(ref patientHediffs);

        return patientHediffs.Any(x => usageBlockingHediffs.Contains(x.def));
    }

    public static bool HasUsageBlockingTraits(Pawn pawn, List<TraitDef> usageBlockingTraits)
    {
        return pawn.story?.traits.allTraits.Any(x => usageBlockingTraits.Contains(x.def)) ?? false;
    }

    public static bool HasImmunizableHediffs(
        Pawn pawn,
        List<HediffDef> inclusions = null,
        List<HediffDef> exclusions = null)
    {
        var result = FindImmunizableHediffs(pawn, inclusions, exclusions);
        return result != null && result.Count > 0;
    }

    public static List<Hediff> FindImmunizableHediffs(
        Pawn pawn,
        List<HediffDef> inclusions = null,
        List<HediffDef> exclusions = null)
    {
        List<Hediff> hediffs = new();
        List<Hediff> allHediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < allHediffs.Count; i++)
        {
            Hediff current = allHediffs[i];

            if (inclusions != null && inclusions.Contains(current.def))
            {
                hediffs.Add(current);
                continue;
            }

            bool isViable = current.Visible
                && current.def.everCurableByItem
                && current.TryGetComp<HediffComp_Immunizable>() != null
                && !current.FullyImmune();
            if (isViable)
            {
                if (exclusions != null
                    && exclusions.Contains(current.def)) continue;

                hediffs.Add(allHediffs[i]);
            }
        }

        return hediffs;
    }

    public static void FixImmunizableHealthConditions(
        Pawn pawn,
        List<HediffDef> inclusions = null,
        List<HediffDef> exclusions = null)
    {
        List<Hediff> hediffs = FindImmunizableHediffs(pawn, inclusions, exclusions);

        if (hediffs == null || hediffs.Count == 0)
            return;

        foreach (var hediff in hediffs)
            Verse.HealthUtility.Cure(hediff);
    }
}
