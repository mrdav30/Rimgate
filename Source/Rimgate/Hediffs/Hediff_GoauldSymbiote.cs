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

        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_KrintakSickness);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_TretoninAddiction);
    }

    public override void PostRemoved()
    {
        if (pawn == null || pawn.health == null)
            return;

        base.PostRemoved();

        if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal))
        {
            var wd = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_SymbioteWithdrawal, pawn);
            pawn.health.AddHediff(wd);
        }
    }
}