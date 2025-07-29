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
        var withdrawalDef = Rimgate_DefOf.Rimgate_SymbioteWithdrawal;

        if (pawn.health.hediffSet.HasHediff(withdrawalDef))
            return;

        var hediff = HediffMaker.MakeHediff(withdrawalDef, pawn);
        pawn.health.AddHediff(hediff);
    }
}