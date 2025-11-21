using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimgate;

public static class MedicalUtil
{
    public static bool HasAllowedMedicalCareCategory(Pawn pawn)
    {
        return pawn != null
            && WorkGiver_DoBill.GetMedicalCareCategory(pawn) >= MedicalCareCategory.NormalOrWorse;
    }

    public static bool HasUsageBlockingHediffs(
        Pawn pawn,
        List<HediffDef> usageBlockingHediffs,
        out List<Hediff> blockingHediffs)
    {
        blockingHediffs = pawn?.health?.hediffSet?.hediffs
            .Where(x => usageBlockingHediffs.Contains(x.def))
            .ToList();

        return blockingHediffs?.Count > 0;
    }

    public static bool HasUsageBlockingTraits(
        Pawn pawn,
        List<TraitDef> usageBlockingTraits,
        out List<Trait> blockingTraits)
    {
        blockingTraits = pawn.story?.traits?.allTraits
            .Where(x => usageBlockingTraits.Contains(x.def))
            .ToList();
        return blockingTraits?.Count > 0;
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
            HealthUtility.Cure(hediff);
    }


    public static bool HasHediff(this Pawn pawn, HediffDef def)
    {
        HediffSet set = pawn?.health?.hediffSet;
        if (set is null || def is null) return false;

        return set.HasHediff(def);
    }

    public static bool HasHediff<T>(this Pawn pawn) where T : Hediff
    {
        HediffSet set = pawn?.health?.hediffSet;
        if (set is null) return false;

        return set.HasHediff<T>();
    }

    public static Hediff GetHediff(this Pawn pawn, HediffDef def)
    {
        HediffSet set = pawn?.health?.hediffSet;
        if (set is null || def is null) return null;

        if (set.TryGetHediff(def, out Hediff result))
            return result;
        return null;
    }

    public static T GetHediff<T>(this Pawn pawn) where T : Hediff
    {
        HediffSet set = pawn?.health?.hediffSet;
        if (set is null) return null;

        if (set.TryGetHediff<T>(out T result))
            return result;
        return null;
    }

    public static Hediff ApplyHediff(
        this Pawn pawn,
        HediffDef hediffDef,
        BodyPartRecord bodyPart,
        int duration,
        float severity)
    {
        Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn, bodyPart);

        if (severity > float.Epsilon)
            hediff.Severity = severity;

        if (hediff is HediffWithComps hediffWithComps)
        {
            foreach (HediffComp comp in hediffWithComps.comps)
            {
                if (duration > 0
                    && comp is HediffComp_Disappears hediffComp_Disappears)
                {
                    hediffComp_Disappears.ticksToDisappear = duration;
                }
            }
        }

        pawn.health.AddHediff(hediff);
        return pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
    }
}
