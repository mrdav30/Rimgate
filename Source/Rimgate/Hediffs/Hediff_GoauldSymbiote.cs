using Verse;

namespace Rimgate;

public class Hediff_GoauldSymbiote : Hediff_Implant
{

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);

        if (pawn == null || pawn.health == null || comps == null)
            return;

        for (int i = 0; i < comps.Count; i++)
            comps[i].CompPostPostAdd(dinfo);
    }

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

    public override bool Visible => false;
}