using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;

namespace Rimgate;

public class Gene_WraithEssenceMetabolism : Gene_Resource, IGeneResourceDrain
{
    public bool FilledPodsAllowed = true;

    public Pawn Pawn => pawn;
    public Gene_Resource Resource => this;

    public bool CanOffset => Active && !pawn.Dead;

    public string DisplayLabel => def.label + " (" + "Gene".Translate() + ")";

    public override float InitialResourceMax => 1f;
    public override float MinLevelForAlert => 0.15f;
    public override float MaxLevelOffset => 0.1f;

    protected override Color BarColor => new Color(0.35f, 0.95f, 0.85f);
    protected override Color BarHighlightColor => new Color(0.45f, 1.0f, 0.95f);

    private static readonly FloatRange Peckish = new FloatRange(0.50f, 0.66f);
    private static readonly FloatRange Hungry = new FloatRange(0.33f, 0.50f);
    private static readonly FloatRange Starving = new FloatRange(0.15f, 0.33f);
    private const float Catastrophic = 0.05f;

    public override void PostAdd()
    {
        base.PostAdd();
        Reset(); // start full
    }

    public float ResourceLossPerDay => def.resourceLossPerDay;

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        GeneResourceDrainUtility.TickResourceDrainInterval(this, delta);
        if (pawn?.health == null || pawn.Dead) return;
        UpdateDeficitHediff();
    }

    public override void SetTargetValuePct(float val)
    {
        targetValue = Mathf.Clamp(val * Max, 0f, Max - MaxLevelOffset);
    }

    public bool ShouldConsumeResourceNow() => Value < targetValue;

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (!Active) yield break;
        foreach (var g in base.GetGizmos()) yield return g;
        foreach (var g in GeneResourceDrainUtility.GetResourceDrainGizmos(this)) yield return g;
    }

    private void UpdateDeficitHediff()
    {
        float pct = Value;

        float sev = 0f;
        if (pct <= 0.05f) sev = 0.95f;
        else if (pct < 0.33f) sev = 0.70f;
        else if (pct < 0.50f) sev = 0.45f;
        else if (pct < 0.66f) sev = 0.20f;

        var def = RimgateDefOf.Rimgate_WraithEssenceDeficit;
        var hediff = pawn.GetHediffOf(def);
        if (sev > 0f)
        {
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(def, pawn);
                hediff.Severity = sev;
                pawn.health.AddHediff(hediff);
            }
            else
                hediff.Severity = Mathf.Lerp(hediff.Severity, sev, 0.2f);
        }
        else if (hediff != null)
            pawn.health.RemoveHediff(hediff);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref FilledPodsAllowed, "FilledPodsAllowed", defaultValue: true);
    }
}