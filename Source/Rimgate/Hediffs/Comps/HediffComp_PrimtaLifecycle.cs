using RimWorld;
using Verse;

namespace Rimgate;

public class HediffComp_PrimtaLifecycle : HediffComp
{
    private const float KrintakThreshold = 0.85f;

    public bool Mature => _matured;

    public HediffCompProperties_PrimtaLifecycle Props => (HediffCompProperties_PrimtaLifecycle)props;

    private int _maturePeriod;

    private int _matureGrace;

    private int _ageTicks;

    private bool _matured;

    private bool _krintakTriggered;

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref _maturePeriod, "_maturePeriod");
        Scribe_Values.Look(ref _matureGrace, "_matureGrace");
        Scribe_Values.Look(ref _ageTicks, "_ageTicks", 0);
        Scribe_Values.Look(ref _matured, "_matured", false);
        Scribe_Values.Look(ref _krintakTriggered, "_krintakTriggered", false);
    }

    public override void CompPostMake()
    {
        base.CompPostMake();

        _maturePeriod = Props.ticksToMature.RandomInRange;
        _matureGrace = Props.ticksAfterMatureGrace.RandomInRange;
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);

        Pawn pawn = parent.pawn;
        if (pawn == null || pawn.Dead || pawn.health == null)
            return;

        // Increment age
        _ageTicks++;

        // Ensure pouch still exists; if not, the main hediff handles cleanup
        if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbiotePouch))
            return;

        if (!_matured)
        {
            if (!_krintakTriggered)
            {
                float pct = (float)_ageTicks / _maturePeriod;
                if (pct >= KrintakThreshold)
                {
                    _krintakTriggered = true;
                    TriggerKrintak(pawn);
                }
            }

            if (_ageTicks >= _maturePeriod)
            {
                _matured = true;
                pawn.RemoveHediffOf(RimgateDefOf.Rimgate_KrintakSickness);
                if (pawn.Faction == Faction.OfPlayer)
                    Messages.Message(
                        "RG_Primta_Matured".Translate(pawn.Named("PAWN")),
                        pawn,
                        MessageTypeDefOf.ThreatSmall);
            }

            return;
        }

        if (_ageTicks < _maturePeriod + _matureGrace)
            return;

        // Check infrequently once mature
        if (Find.TickManager.TicksGame % 60000 == 0)
            EvaluateOverstayOutcome(pawn);
    }

    private void TriggerKrintak(Pawn pawn)
    {
        // Apply the sickness
        if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_KrintakSickness))
        {
            var h = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_KrintakSickness, pawn);
            pawn.health.AddHediff(h);
        }

        // Send important letter
        if (pawn.Faction == Faction.OfPlayer)
        {
            Find.LetterStack.ReceiveLetter(
                "RG_Primta_KrintakLabel".Translate(pawn.Named("PAWN")),
                "RG_Primta_KrintakText".Translate(pawn.Named("PAWN")),
                LetterDefOf.NegativeEvent,
                pawn);
        }
    }

    private void EvaluateOverstayOutcome(Pawn pawn)
    {
        // If pawn already has a mature symbiote, do nothing
        if (pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteImplant))
            return;

        // Roll: takeover or mutual destruction
        //   - Takeover: prim'ta becomes mature Goa'uld symbiote in the host
        //   - Both die: outright kill
        if (Rand.Value < Props.takeoverChance)
            BecomeGoauld(pawn);
        else
            KillHostAndPrimta(pawn);
    }

    private void BecomeGoauld(Pawn pawn)
    {
        if (parent is Hediff_PrimtaInPouch primta)
            primta.MarkInternalRemoval();

        // Remove prim'ta hediff
        pawn.health.RemoveHediff(parent);

        // Remove pouch
        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_SymbiotePouch);

        // Add mature symbiote
        var sym = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_SymbioteImplant, pawn);
        pawn.health.AddHediff(sym);

        if (pawn.Faction == Faction.OfPlayer)
        {
            Messages.Message(
                "RG_Primta_Takeover".Translate(pawn.Named("PAWN")),
                pawn,
                MessageTypeDefOf.ThreatBig);
        }
    }

    private void KillHostAndPrimta(Pawn pawn)
    {
        if (parent is Hediff_PrimtaInPouch primta)
            primta.MarkInternalRemoval();

        pawn.Kill(null);

        if (pawn.Faction == Faction.OfPlayer)
        {
            Messages.Message(
                "RG_Primta_FatalOverstay".Translate(pawn.Named("PAWN")),
                pawn,
                MessageTypeDefOf.ThreatBig);
        }
    }
}
