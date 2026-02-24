using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class HediffComp_PrimtaLifecycle : HediffComp
{
    public const int TicksPerDay = 60000;

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

    public override string CompLabelInBracketsExtra
    {
        get
        {
            if (parent?.pawn == null || _maturePeriod <= 0)
                return null;

            if (!_matured)
            {
                if (MaturityPct > 0f)
                {
                    int pct = Mathf.Clamp(Mathf.RoundToInt(MaturityPct * 100f), 0, 100);
                    return pct + "%";
                }

                return "brood";
            }

            return "mature";
        }
    }

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

        if (!pawn.IsHashIntervalTick(GenTicks.TickRareInterval))
            return;

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
                MarkMature(pawn);

            return;
        }

        if (!_matured)
            return;

        if (_ageTicks < _maturePeriod + _matureGrace)
            return;

        // If the prim'ta has matured and stayed past the grace period, evaluate outcomes every in-game day
        if (Find.TickManager.TicksGame % TicksPerDay == 0)
            EvaluateOverstayOutcome(pawn);
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        Pawn pawn = parent?.pawn;
        if (!Prefs.DevMode
            || pawn == null
            || pawn.Dead
            || pawn.Faction?.IsOfPlayerFaction() != true)
            yield break;

        Command_Action cmd = new()
        {
            defaultLabel = "DEV: Mature Prim'ta",
            defaultDesc = "Force this prim'ta into mature state immediately for testing.",
            action = () => MarkMature(pawn)
        };

        if (_matured)
            cmd.Disable("Prim'ta is already mature.");

        yield return cmd;

        Command_Action forceTakeover = new()
        {
            defaultLabel = "DEV: Overstay -> Takeover",
            defaultDesc = "Force the mature prim'ta overstay outcome where it takes over the host.",
            action = () => BecomeGoauld(pawn)
        };

        if (!_matured)
            forceTakeover.Disable("Prim'ta must be mature.");
        else if (pawn.HasHediffOf(RimgateDefOf.Rimgate_SymbioteImplant))
            forceTakeover.Disable("Pawn already has a symbiote implant.");

        yield return forceTakeover;

        Command_Action forceFatal = new()
        {
            defaultLabel = "DEV: Overstay -> Fatal",
            defaultDesc = "Force the mature prim'ta overstay outcome where host and prim'ta both die.",
            action = () => KillHostAndPrimta(pawn)
        };

        if (!_matured)
            forceFatal.Disable("Prim'ta must be mature.");

        yield return forceFatal;
    }

    private void MarkMature(Pawn pawn)
    {
        if (pawn == null || _matured)
            return;

        _matured = true;
        _krintakTriggered = true;
        _ageTicks = Mathf.Max(_ageTicks, _maturePeriod);

        pawn.RemoveHediffOf(RimgateDefOf.Rimgate_KrintakSickness);
        pawn.TryGiveThought(RimgateDefOf.Rimgate_PrimtaMaturedThought);

        if (pawn.Faction.IsOfPlayerFaction())
        {
            Messages.Message(
                "RG_Primta_Matured".Translate(pawn.Named("PAWN")),
                pawn,
                MessageTypeDefOf.ThreatSmall);
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
        SymbioteQueenLineage lineage = null;
        if (parent is Hediff_PrimtaInPouch primta)
        {
            primta.MarkInternalRemoval();
            lineage = primta.QueenLineage;
        }

        // Remove the Prim'ta
        pawn.health.RemoveHediff(parent);

        // Add mature symbiote, will remove the pouch hediff
        var sym = HediffMaker.MakeHediff(RimgateDefOf.Rimgate_SymbioteImplant, pawn);
        if (sym is Hediff_SymbioteImplant implant)
            implant.Heritage?.AssumeQueenLineage(lineage);
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
