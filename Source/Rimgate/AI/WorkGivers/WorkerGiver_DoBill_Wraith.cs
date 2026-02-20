using RimWorld;
using Verse;

namespace Rimgate;

public class WorkGiver_DoBillWraith : WorkGiver_DoBill
{
    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (!pawn.HasActiveGeneOf(RimgateDefOf.Rimgate_WraithWebbingGene))
            return true;

        return base.ShouldSkip(pawn, forced);
    }
}
