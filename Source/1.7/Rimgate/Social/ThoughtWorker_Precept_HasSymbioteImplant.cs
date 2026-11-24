using RimWorld;
using Verse;

namespace Rimgate;

public class ThoughtWorker_Precept_HasSymbioteImplant : ThoughtWorker_Precept
{
    protected override ThoughtState ShouldHaveThought(Pawn p)
    {
        if (!p.RaceProps.Humanlike)
            return ThoughtState.Inactive;

        if (!p.HasSymbiote())
            return ThoughtState.Inactive;

        return ThoughtState.ActiveDefault;
    }
}
