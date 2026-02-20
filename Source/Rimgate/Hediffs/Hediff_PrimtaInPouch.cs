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

        bool isConfigStage = Current.ProgramState != ProgramState.Playing;

        if (!IsValidHost(out string reason))
        {
            if (pawn.Map != null)
            {
                var thing = ThingMaker.MakeThing(RimgateDefOf.Rimgate_PrimtaSymbiote);
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
            }

            if (!isConfigStage && pawn.Faction.IsOfPlayerFaction())
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

        ThoughtDef thought = watcher.EverHadPrimta
            ? RimgateDefOf.Rimgate_PrimtaNewPrimtaThought
            : RimgateDefOf.Rimgate_PrimtaFirstPrimtaThought;

        pawn.TryGiveThought(thought);

        if (!isConfigStage)
            RimgateEvents.Notify_ColonyOfPawnEvent(pawn, RimgateDefOf.Rimgate_InstalledSymbiote);
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

        if (_immediateRejection
            || pawn == null
            || pawn.health == null
            || pawn.Dead) return;

        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_KrintakSickness);

        if (pawn.Map != null)
        {
            var def = Lifecycle?.Mature == true
                ? RimgateDefOf.Rimgate_GoauldSymbiote
                : RimgateDefOf.Rimgate_PrimtaSymbiote;
            var thing = ThingMaker.MakeThing(def);
            if (pawn.Map != null)
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
        }

        if (pawn.needs.TryGetNeed(RimgateDefOf.Rimgate_TretoninChemicalNeed) == null
            && !pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal))
        {
            var wd = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_SymbioteWithdrawal, pawn);
            pawn.health.AddHediff(wd);
        }

        var memories = pawn.needs?.mood?.thoughts?.memories;
        if (memories == null) return;

        memories.RemoveMemoriesOfDef(RimgateDefOf.Rimgate_PrimtaMaturedThought);
    }

    public override void Notify_PawnKilled()
    {
        // Prim'ta absorbed into body upon death
        _immediateRejection = true;
        base.Notify_PawnKilled();
        pawn.RemoveHediff(this);
    }
}
