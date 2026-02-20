using RimWorld;
using Verse;

namespace Rimgate;

public enum OutcomeAffects
{
    Caster,
    Target
}

public class CompProperties_ApplyDrainLife : CompProperties_AbilityEffect
{
    public IntRange aiRangeLimit = IntRange.Invalid;

    public int MinRangeSquared => aiRangeLimit.min * aiRangeLimit.min;

    public int MaxRangeSquared => aiRangeLimit.max * aiRangeLimit.max;

    public GeneDef geneDef;

    public float essenceGainAmount = 0.05f;       // +5% by default

    public OutcomeAffects affects = OutcomeAffects.Caster;

    public bool allowFreeDraw = false;  // allow gain even if no drain occurred

    public HediffDef hediffToReceive;

    public float hediffToReceiveSeverity = -1;

    public HediffDef hediffToGive;

    public float hediffToGiveSeverity = -1;

    public ThoughtDef thoughtToGiveTarget;

    public ThoughtDef opinionThoughtToGiveTarget;

    public CompProperties_ApplyDrainLife() => compClass = typeof(CompAbilityEffect_ApplyDrainLife);
}
