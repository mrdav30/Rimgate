using RimWorld;
using Verse;

namespace Rimgate;

public class Hediff_SymbioteImplant : Hediff_Implant
{
    public override bool Visible => true;

    private bool _skipWithdrawl;

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);

        // Safety: don't allow pawns that already have a symbiote
        if (!IsValidHost(out string reason))
        {
            // Spawn mature symbiote item at pawn's position
            if (pawn.Map != null)
            {
                var thing = ThingMaker.MakeThing(RimgateDefOf.Rimgate_GoauldSymbiote);
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
            }

            if (pawn.Faction == Faction.OfPlayer)
                Messages.Message(
                    reason,
                    pawn,
                    MessageTypeDefOf.ThreatBig);

            _skipWithdrawl = true;

            pawn.health.RemoveHediff(this);

            return;
        }

        // Mature symbiote will remove the pouch
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_SymbiotePouch);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_PouchDegeneration);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_KrintakSickness);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_TretoninAddiction);
    }

    public bool IsValidHost(out string reason)
    {
        reason = null;

        if (pawn.HasHediffOf(RimgateDefOf.Rimgate_PrimtaInPouch))
        {
            reason = "RG_RejectHost_HasSymbiote".Translate(pawn.Named("PAWN"));
            return false;
        }

        return true;
    }

    public override void PostRemoved()
    {
        base.PostRemoved();

        if (pawn == null || pawn.health == null)
            return;

        // If this was a rejection or internal event we flagged, skip spawn + withdrawal
        if (_skipWithdrawl)
            return;

        if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal))
        {
            var wd = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_SymbioteWithdrawal, pawn);
            pawn.health.AddHediff(wd);
        }
    }
}