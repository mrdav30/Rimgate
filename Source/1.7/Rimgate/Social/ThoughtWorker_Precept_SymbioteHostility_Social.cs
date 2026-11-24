using RimWorld;
using Verse;

namespace Rimgate;

public class ThoughtWorker_Precept_SymbioteHostility_Social : ThoughtWorker_Precept_Social
{
    protected override ThoughtState ShouldHaveThought(Pawn p, Pawn other)
    {
        if (!Utils.CanSocialize(p, other))
            return ThoughtState.Inactive;

        // Only non-host pawns get the *uneasy* opinion of a host.
        bool otherIsHost = other.HasSymbiote();
        bool selfIsHost = p.HasSymbiote();

        if (otherIsHost && !selfIsHost)
            return ThoughtState.ActiveDefault;

        return ThoughtState.Inactive;
    }
}
