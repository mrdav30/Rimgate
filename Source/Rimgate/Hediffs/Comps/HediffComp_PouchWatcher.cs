using RimWorld;
using Verse;

namespace Rimgate;

public class HediffComp_PouchWatcher : HediffComp
{
    public bool EverHadPrimta => _everHadPrimta;
    public int FatalGraceTicks => Props.fatalGraceDays * GenDate.TicksPerDay;
    public HediffCompProperties_PouchWatcher Props => (HediffCompProperties_PouchWatcher)props;

    private bool _everHadPrimta;

    private bool _adultNotificationSent;
    private int _ticksSinceAdultNoSupport;

    public override void CompExposeData()
    {
        Scribe_Values.Look(ref _adultNotificationSent, "_adultNotificationSent", false);
        Scribe_Values.Look(ref _ticksSinceAdultNoSupport, "_ticksSinceAdultNoSupport", 0);
        Scribe_Values.Look(ref _everHadPrimta, "_everHadPrimta", false);
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
        if (pawn == null 
            || !pawn.Faction.IsOfPlayerFaction()
            || pawn.Dead 
            || !pawn.Spawned
            || pawn.Map == null
            || pawn.health == null) return;

        if (!pawn.IsHashIntervalTick(2500))
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
        bool hasWithdrawal = pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteWithdrawal);

        if (hasPrimta)
            _everHadPrimta = true;

        bool hasAnySupport = hasPrimta || hasTretonin;

        // Only the never-hosted path uses PouchDegeneration
        if (!hasAnySupport && !hasWithdrawal)
        {
            if (!_adultNotificationSent)
            {
                _adultNotificationSent = true;
                SendAdultNeedsPrimtaLetter(pawn);
            }

            _ticksSinceAdultNoSupport += 2500;

            if (_ticksSinceAdultNoSupport >= FatalGraceTicks
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
