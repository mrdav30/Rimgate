using RimWorld;
using Verse;

namespace Rimgate;

public class HediffComp_PouchWatcher : HediffComp
{
    private bool _adultNotificationSent;

    private int _ticksSinceAdultNoSupport;

    public HediffCompProperties_PouchWatcher Props => (HediffCompProperties_PouchWatcher)props;

    public int FatalGraceDays => Props.fatalGraceDays * GenDate.TicksPerDay;

    public override void CompExposeData()
    {
        Scribe_Values.Look(ref _adultNotificationSent, "_adultNotificationSent", false);
        Scribe_Values.Look(ref _ticksSinceAdultNoSupport, "_ticksSinceAdultNoSupport", 0);
    }

    public override void CompPostPostRemoved()
    {
        Pawn pawn = parent.pawn;
        if (pawn == null || pawn.health == null || pawn.Map == null)
            return;

        var primta = pawn.GetHediffOf(RimgateDefOf.Rimgate_PrimtaInPouch);
        if (primta == null)
            return;

        pawn.health.RemoveHediff(primta);

        var thing = ThingMaker.MakeThing(RimgateDefOf.Rimgate_PrimtaSymbiote);
        GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        var pawn = parent.pawn;
        if (pawn == null || pawn.Dead || pawn.health == null)
            return;

        if (!pawn.IsHashIntervalTick(2500))
            return;

        if (!pawn.Spawned || pawn.Map == null)
            return;

        // If no age tracker present, assume they're and adult
        bool isAdult = pawn.ageTracker?.Adult ?? true;
        if (!isAdult)
        {
            _ticksSinceAdultNoSupport = 0;
            return;
        }

        bool hasPrimta = pawn.HasHediffOf(RimgateDefOf.Rimgate_PrimtaInPouch);
        bool hasTretonin = pawn.HasHediffOf(RimgateDefOf.Rimgate_TretoninAddiction);

        // Support = anything that substitutes for the Prim'ta role.
        bool hasAnySupport = hasPrimta || hasTretonin;

        bool hasWithdrawal = pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal);

        // 2) Death timer: only for *never hosted* Jaffa:
        //    pouch present + no prim'ta + no withdrawal + no tretonin
        if (!hasAnySupport && !hasWithdrawal)
        {
            // 1) On becoming adult with no support, send the needs Prim'ta letter once,
            // if they are in withdrawal, assume they had a prior symbiote
            if (!_adultNotificationSent)
            {
                _adultNotificationSent = true;
                SendAdultNeedsPrimtaLetter(pawn);
            }

            _ticksSinceAdultNoSupport += 2500;

            if (_ticksSinceAdultNoSupport >= FatalGraceDays
                && !pawn.HasHediffOf(RimgateDefOf.Rimgate_PouchDegeneration))
            {
                var h = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_PouchDegeneration, pawn);
                pawn.health.AddHediff(h);
            }

            return;
        }

        _ticksSinceAdultNoSupport = 0;
    }

    private void SendAdultNeedsPrimtaLetter(Pawn pawn)
    {
        string label = "RG_PouchAdult_PrimtaNeeded_Label".Translate();
        string text = "RG_PouchAdult_PrimtaNeeded_Text".Translate(pawn.Named("PAWN"));
        Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatSmall, pawn);
    }
}
