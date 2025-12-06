using RimWorld;
using Verse;

namespace Rimgate;

public class Hediff_PrimtaInPouch : Hediff_Implant
{
    public const int MaxPrimtaHostAge = 110;

    public override bool Visible => true;

    public HediffComp_PrimtaLifecycle Lifecycle => _lifecycle ??= GetComp<HediffComp_PrimtaLifecycle>();

    private bool _immediateRejection;

    private HediffComp_PrimtaLifecycle _lifecycle;

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);

        // Safety: only allow on pawns that have the symbiote pouch hediff and no existing symbiote
        if (!IsValidHost(out string reason))
        {
            // Spawn prim'ta item at pawn's position
            if (pawn.Map != null)
            {
                var thing = ThingMaker.MakeThing(RimgateDefOf.Rimgate_PrimtaSymbiote);
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
            }

            if (pawn.Faction == Faction.OfPlayer)
                Messages.Message(
                    reason,
                    pawn,
                    MessageTypeDefOf.ThreatSmall);

            _immediateRejection = true;

            pawn.health.RemoveHediff(this);

            return;
        }

        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_PouchDegeneration);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_KrintakSickness);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal);
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_TretoninAddiction);

        var pouch = pawn.GetHediffOf(RimgateDefOf.Rimgate_SymbiotePouch);
        var watcher = pouch?.TryGetComp<HediffComp_PouchWatcher>();
        if (watcher == null) return;

        var memories = pawn.needs?.mood?.thoughts?.memories;
        if (memories == null) return;

        ThoughtDef thought;
        if (watcher.EverHadPrimta) // Already had one in the past
            thought = RimgateDefOf.Rimgate_PrimtaNewPrimtaThought;
        else // First ever Prim'ta for this pouch
            thought = RimgateDefOf.Rimgate_PrimtaFirstPrimtaThought;

        memories.TryGainMemory(thought);
    }

    public bool IsValidHost(out string reason)
    {
        reason = null;

        if (pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteImplant))
        {
            reason = "RG_RejectHost_HasSymbiote".Translate(pawn.Named("PAWN"));
            return false;
        }

        if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbiotePouch))
        {
            reason = "RG_RejectHost_NoPouch".Translate(pawn.Named("PAWN"));
            return false;
        }

        if (pawn.ageTracker.AgeBiologicalYears >= MaxPrimtaHostAge)
        {
            reason = "RG_RejectHost_TooOld".Translate(pawn.Named("PAWN"));
            return false;
        }

        return true;
    }

    public void MarkInternalRemoval() => _immediateRejection = true;

    public override void PostRemoved()
    {
        base.PostRemoved();

        if (pawn == null || pawn.health == null)
            return;

        // If this was a rejection or internal event we flagged, skip spawn + withdrawal
        if (_immediateRejection)
            return;

        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_KrintakSickness);

        if (pawn.Map != null)
        {
            var def = Lifecycle?.Mature == true
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

        var memories = pawn.needs?.mood?.thoughts?.memories;
        if (memories == null) return;

        memories.RemoveMemoriesOfDef(RimgateDefOf.Rimgate_PrimtaMaturedThought);
    }
}
