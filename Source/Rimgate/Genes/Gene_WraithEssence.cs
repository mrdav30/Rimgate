using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;

namespace Rimgate;

public class Gene_WraithEssence : Gene_Resource, IGeneResourceDrain
{
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
        var he = pawn.GetHediffOf(def);
        if (sev > 0f)
        {
            if (he == null)
            {
                he = HediffMaker.MakeHediff(def, pawn);
                he.Severity = sev;
                pawn.health.AddHediff(he);
            }
            else
                he.Severity = Mathf.Lerp(he.Severity, sev, 0.2f);
        }
        else if (he != null)
            pawn.health.RemoveHediff(he);
    }
}