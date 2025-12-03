using RimWorld;
using System.Collections.Generic;
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

    public static Gene GetActiveGene(this Pawn pawn, GeneDef def)
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
}
