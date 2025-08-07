using Verse;

namespace Rimgate;

public class Hediff_GoauldSymbiote : Hediff_Implant
{
    public override bool Visible => true;

    public override void PostAdd(DamageInfo? dinfo)
    {
        if (pawn == null || pawn.health == null)
            return;

        base.PostAdd(dinfo);

        for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
        {
            Hediff hediff = pawn.health.hediffSet.hediffs[i];
            if (hediff.def == Rimgate_DefOf.Rimgate_SymbioteWithdrawal)
                pawn.health.RemoveHediff(hediff);
        }
    }

    public override void PostRemoved()
    {
        if (pawn == null || pawn.health == null)
            return;

        base.PostRemoved();

        // Apply fatal withdrawal hediff
        if (pawn.health.hediffSet.HasHediff(Rimgate_DefOf.Rimgate_SymbioteWithdrawal))
            return;

        var hediff = HediffMaker.MakeHediff(Rimgate_DefOf.Rimgate_SymbioteWithdrawal, pawn);
        pawn.health.AddHediff(hediff);
    }
}