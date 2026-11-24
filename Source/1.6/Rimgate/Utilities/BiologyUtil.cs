using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public static class BiologyUtil
{
    public static bool HasActiveGeneOf(this Pawn pawn, GeneDef geneDef)
    {
        if (geneDef is null) return false;
        if (pawn.genes is null) return false;
        return pawn.genes.GetGene(geneDef)?.Active ?? false;
    }

    public static bool HasActiveGeneOf(this Pawn pawn, string def)
    {
        if (string.IsNullOrEmpty(def)) return false;
        if (pawn.genes is null) return false;
        return pawn.genes?.GetGene(DefDatabase<GeneDef>.GetNamedSilentFail(def))?.Active ?? false;
    }

    public static Gene GetActiveGeneOf(this Pawn pawn, GeneDef def)
    {
        if (pawn.genes is null) return null;
        var gene = pawn.genes.GetGene(def);
        return gene == null || !gene.Active
            ? null
            : gene;
    }

    public static T GetActiveGene<T>(this Pawn pawn) where T : Gene
    {
        if (pawn.genes is null) return null;
        var gene = pawn.genes.GetFirstGeneOfType<T>();
        return gene == null || !gene.Active
            ? null
            : gene;
    }

    public static Gene AddGene(this Pawn pawn, GeneDef geneDef, bool xenogene = false)
    {
        if (pawn.genes is null) return null;
        if (pawn.GetActiveGeneOf(geneDef) != null) return null;
        return pawn.genes.AddGene(geneDef, xenogene);
    }

    public static bool IsXenoTypeOf(this Pawn pawn, XenotypeDef xenotypeDef)
    {
        if (xenotypeDef is null) return false;
        if (pawn.genes is null) return false;
        return pawn.genes.Xenotype == xenotypeDef;
    }

    public static bool IsValidRaceFor(this Pawn pawn, List<string> disallowedRaces)
    {
        string race = pawn.def.ToString();
        return disallowedRaces.NullOrEmpty() || !disallowedRaces.Contains(race);
    }

    public static bool IsValidXenotypeFor(this Pawn pawn, List<XenotypeDef> disallowedXenotypes)
    {
        XenotypeDef xenotype = pawn.genes?.Xenotype;
        return xenotype == null
            || disallowedXenotypes.NullOrEmpty()
            || !disallowedXenotypes.Contains(xenotype);
    }

    public static void OffsetEssenceCost(Pawn pawn, float offset, bool applyStatFactor = true)
    {
        if (!ModsConfig.BiotechActive)
            return;

        if (offset > 0f && applyStatFactor)
            offset *= Mathf.Clamp01(1 - pawn.GetStatValue(RimgateDefOf.Rimgate_EssenceCostFactor));

        Gene_WraithEssenceMetabolism geneEssence = pawn.GetActiveGene<Gene_WraithEssenceMetabolism>();
        if (geneEssence != null)
            geneEssence.Value += offset;
    }

    public static void AdjustResourceGain(
        Pawn caster,
        Pawn target,
        GeneDef geneDef,
        float essenceGainAmount,
        bool allowFreeDraw = false,
        OutcomeAffects affects = OutcomeAffects.Target,
        bool applyStatFactor = true)

    {
        if (geneDef == null || essenceGainAmount <= 0f) return;

        // Determine who gets the gain and who (optionally) gets drained.
        Pawn gainer = affects == OutcomeAffects.Target
            ? target
            : caster;
        Pawn drainer = affects == OutcomeAffects.Target
            ? caster
            : target;

        // Apply gain to gainer if they have the resource gene.
        var gainerGene = gainer?.GetActiveGeneOf(geneDef) as Gene_Resource;
        var drainerGene = drainer?.GetActiveGeneOf(geneDef) as Gene_Resource;

        // Nothing to do if gainer lacks the gene.
        if (gainerGene == null) return;

        // How much could the gainer actually accept?
        float factor = applyStatFactor
            ? Mathf.Clamp01(gainer.GetStatValue(RimgateDefOf.Rimgate_EssenceGainFactor))
            : 0f;
        float want = Mathf.Ceil(essenceGainAmount * (1 + factor));
        float canAccept = Mathf.Max(0f, (gainerGene.Max - gainerGene.Value - gainerGene.MaxLevelOffset));
        float gain = Mathf.Min(want, canAccept);

        // If the other pawn has the same resource,
        // drain up to the intended gain.
        if (drainerGene != null && gain > 0f)
        {
            float available = Mathf.Max(0f, drainerGene.Value);
            float drain = Mathf.Min(gain, available);

            // If there is no resource to drain,
            // return if free draw is disallowed
            if (drain > 0)
            {
                drainerGene.Value = Mathf.Max(0f, drainerGene.Value - drain);
                // match the gainer’s increase to what we actually drained
                gain = drain;
            }
            else if (!allowFreeDraw) return;
        }

        if (gain > 0f)
            gainerGene.Value = Mathf.Min(gainerGene.Max - gainerGene.MaxLevelOffset, gainerGene.Value + gain);
    }
}
