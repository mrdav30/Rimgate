using RimWorld;
using Verse;

namespace Rimgate;

public class Hediff_PrimtaInPouch : Hediff_Implant
{
    public override bool Visible => true;

    public HediffComp_PrimtaLifecycle Lifecyle => _lifecycle ??= GetComp<HediffComp_PrimtaLifecycle>();

    private bool _skipWithdrawl;

    private HediffComp_PrimtaLifecycle _lifecycle;

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);

        if (pawn == null || pawn.health == null)
            return;

        // Safety: only allow on pawns that have the symbiote pouch hediff
        if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbiotePouch))
        {
            // No pouch? Immediately remove and drop item.
            pawn.health.RemoveHediff(this);

            // Spawn prim'ta item at pawn's position
            if (pawn.Map != null)
            {
                var thing = ThingMaker.MakeThing(RimgateDefOf.Rimgate_PrimtaSymbiote);
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
            }

            if (pawn.Faction == Faction.OfPlayer)
                Messages.Message(
                    "RG_Primta_RejectHost".Translate(pawn.Named("PAWN")),
                    pawn,
                    MessageTypeDefOf.ThreatBig);

            _skipWithdrawl = true;

            return;
        }

        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_KrintakSickness);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_TretoninAddiction);
    }

    public void MarkInternalRemoval() => _skipWithdrawl = true;

    public override void PostRemoved()
    {
        base.PostRemoved();

        if (pawn == null || pawn.health == null)
            return;

        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_KrintakSickness);

        // If this was a rejection or internal event we flagged, skip spawn + withdrawal
        if (_skipWithdrawl)
            return;

        if (pawn.Map != null)
        {
            var def = _lifecycle.Mature
                ? RimgateDefOf.Rimgate_GoauldSymbiote
                : RimgateDefOf.Rimgate_PrimtaSymbiote;
            var thing = ThingMaker.MakeThing(def);
            GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
        }

        if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal))
        {
            var wd = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_SymbioteWithdrawal, pawn);
            pawn.health.AddHediff(wd);
        }
    }
}
