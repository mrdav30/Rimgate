using Verse;

namespace Rimgate;

public class HediffGiver_AtAge : HediffGiver
{
    public int age;

    public override void OnIntervalPassed(Pawn pawn, Hediff cause)
    {
        if (pawn == null) return;

        int pawnAge = pawn.ageTracker.AgeBiologicalYears;
        if (pawnAge >= age && TryApply(pawn))
            SendLetter(pawn, cause);
    }
}
