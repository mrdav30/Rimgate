using RimWorld;
using Verse;

namespace Rimgate;

public class ThoughtWorker_WraithEssenceCraving : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!p.RaceProps.Humanlike
            || p.Dead
            || p.genes == null) return ThoughtState.Inactive;

        Gene_WraithEssenceMetabolism essence = p.GetActiveGene<Gene_WraithEssenceMetabolism>();
        if (essence == null)
            return ThoughtState.Inactive;

        return p.HasHediffOf(RimgateDefOf.Rimgate_WraithEssenceDeficit);
    }
}
