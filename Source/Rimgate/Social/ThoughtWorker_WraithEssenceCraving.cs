using RimWorld;
using Verse;

namespace Rimgate;

public class ThoughtWorker_WraithEssenceCraving : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (p?.genes == null || p.Dead) return ThoughtState.Inactive;

        Gene_WraithEssence essence = p?.GetActiveGene<Gene_WraithEssence>();
        if (essence == null) 
            return ThoughtState.Inactive;

        return p?.HasHediffOf(RimgateDefOf.Rimgate_WraithEssenceDeficit) == true;
    }
}
