using Verse;

namespace Rimgate;

public class Hediff_GoauldSymbiote : Hediff_Implant
{
    public override bool Visible => true;

    public override void PostRemoved()
    {
        base.PostRemoved();

        if (pawn == null || pawn.health == null)
            return;

        // Apply fatal withdrawal hediff
        if (pawn.health.hediffSet.HasHediff(Rimgate_DefOf.Rimgate_SymbioteWithdrawal))
            return;

        var hediff = HediffMaker.MakeHediff(Rimgate_DefOf.Rimgate_SymbioteWithdrawal, pawn);
        pawn.health.AddHediff(hediff);
    }
}