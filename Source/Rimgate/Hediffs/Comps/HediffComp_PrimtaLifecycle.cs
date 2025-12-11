using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class HediffComp_PrimtaLifecycle : HediffComp
{
    public const float KrintakThreshold = 0.85f;

    public bool Mature => _matured;

    public HediffCompProperties_PrimtaLifecycle Props => (HediffCompProperties_PrimtaLifecycle)props;

    public float MaturityPct =>
        _maturePeriod <= 0 
        ? 0f 
        : Mathf.Clamp01((float)_ageTicks / _maturePeriod);

    private int _maturePeriod;
    private int _matureGrace;
    private int _ageTicks;
    private bool _matured;
    private bool _krintakTriggered;

    public override void CompExposeData()
    {
        Scribe_Values.Look(ref _maturePeriod, "_maturePeriod");
        Scribe_Values.Look(ref _matureGrace, "_matureGrace");
        Scribe_Values.Look(ref _ageTicks, "_ageTicks", 0);
        Scribe_Values.Look(ref _matured, "_matured", false);
        Scribe_Values.Look(ref _krintakTriggered, "_krintakTriggered", false);
    }

    public override void CompPostMake()
    {
        _maturePeriod = Props.ticksToMature.RandomInRange;
        _matureGrace = Props.ticksAfterMatureGrace.RandomInRange;
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        Pawn pawn = parent.pawn;
        if (pawn == null 
            || !pawn.Faction.IsOfPlayerFaction()
            || pawn.Dead 
            || pawn.health == null) return;

        _ageTicks++;

        // Pouch must still exist
        if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbiotePouch))
            return;

        if (!_matured)
        {
            if (!_krintakTriggered)
            {
                if (MaturityPct >= KrintakThreshold)
                {
                    _krintakTriggered = true;
                    TriggerKrintak(pawn);
                }
            }

            if (_ageTicks >= _maturePeriod)
            {
                _matured = true;
                pawn.RemoveHediffOf(RimgateDefOf.Rimgate_KrintakSickness);

                pawn.TryGiveThought(RimgateDefOf.Rimgate_PrimtaMaturedThought);

                if (pawn.Faction.IsOfPlayerFaction())
                    Messages.Message(
                        "RG_Primta_Matured".Translate(pawn.Named("PAWN")),
                        pawn,
                        MessageTypeDefOf.ThreatSmall);
            }

            return;
        }

        if (_ageTicks < _maturePeriod + _matureGrace)
            return;

        if (Find.TickManager.TicksGame % 60000 == 0)
            EvaluateOverstayOutcome(pawn);
    }

    public override string CompLabelInBracketsExtra 
    {
        get
        {
            if (parent?.pawn == null || _maturePeriod <= 0)
                return null;

            if (!_matured && MaturityPct > 0f)
                return MaturityPct.ToStringPercent();

            return MaturityPct.ToStringPercent();
        }
    }

    private void TriggerKrintak(Pawn pawn)
    {
        if (!pawn.HasHediffOf(RimgateDefOf.Rimgate_KrintakSickness))
        {
            var h = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_KrintakSickness, pawn);
            pawn.health.AddHediff(h);
        }

        if (pawn.Faction.IsOfPlayerFaction())
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
        if (pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteImplant))
            return;

        if (Rand.Value < Props.takeoverChance)
            BecomeGoauld(pawn);
        else
            KillHostAndPrimta(pawn);
    }

    private void BecomeGoauld(Pawn pawn)
    {
        if (parent is Hediff_PrimtaInPouch primta)
            primta.MarkInternalRemoval();

        // Remove the Prim'ta
        pawn.health.RemoveHediff(parent);

        // Add mature symbiote, will remove the pouch hediff
        var sym = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_SymbioteImplant, pawn);
        pawn.health.AddHediff(sym);

        if (pawn.Faction.IsOfPlayerFaction())
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

        if (pawn.Faction.IsOfPlayerFaction())
        {
            Messages.Message(
                "RG_Primta_FatalOverstay".Translate(pawn.Named("PAWN")),
                pawn,
                MessageTypeDefOf.ThreatBig);
        }
    }
}
