using RimWorld;
using Verse;

namespace Rimgate;

public class ThoughtWorker_SymbioteKinship_Social : ThoughtWorker
{
    protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
    {
        if (!Utils.CanSocialize(p, other))
            return ThoughtState.Inactive;

        // Apply only if both are a host
        bool pHost = p.HasSymbiote();
        bool otherHost = other.HasSymbiote();

        if (pHost && otherHost)
            return ThoughtState.ActiveDefault;

        return ThoughtState.Inactive;
    }
}
