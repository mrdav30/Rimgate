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
            bool hasPouch = p.HasHediffOf(RimgateDefOf.Rimgate_SymbiotePouch);
            if (!hasPouch) return ThoughtState.Inactive;

            bool hasSymbiote = p.HasSymbiote();
            return !hasSymbiote 
                ? ThoughtState.ActiveDefault 
                : ThoughtState.Inactive;
        }
    }
}