using Verse;
using RimWorld;

namespace Rimgate
{
    /// <summary>
    /// Mood debuff when a pawn has a symbiote pouch but no symbiote.
    /// Disables itself if the pouch is missing or a symbiote is present.
    /// </summary>
    public class ThoughtWorker_SymbiotePouchEmpty : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.RaceProps.Humanlike)
                return ThoughtState.Inactive;

            bool hasPouch = p.HasHediffOf(RimgateDefOf.Rimgate_SymbiotePouch);
            if (!hasPouch) 
                return ThoughtState.Inactive;

            bool hasPrimta = p.HasHediffOf(RimgateDefOf.Rimgate_PrimtaInPouch);
            return !hasPrimta;
        }
    }
}