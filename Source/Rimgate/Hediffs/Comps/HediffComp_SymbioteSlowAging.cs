using Verse;

namespace Rimgate;

public class HediffComp_SymbioteSlowAging : HediffComp
{
    public HediffCompProperties_SymbioteSlowAging Props => (HediffCompProperties_SymbioteSlowAging)props;

    private bool _initialized = false;
    private long _initialAgeTicks;

    // Cache once to avoid boxing allocations
    public Pawn Pawn => parent.pawn;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);

        if (Pawn.Dead || Props == null)
            return;

        if (!_initialized)
        {
            _initialAgeTicks = Pawn.ageTracker.AgeBiologicalTicks;
            _initialized = true;
        }

        // Apply anti-aging once per year (default RimWorld year: 3600000 ticks)
        if (Find.TickManager.TicksGame % 3600000 == 0)
            ApplyAgeReduction();
    }

    private void ApplyAgeReduction()
    {
        // Reduce biological age growth by applying a lifespan factor
        long current = Pawn.ageTracker.AgeBiologicalTicks;
        long expected = (long)((current - _initialAgeTicks) / Props.lifespanFactor);
        Pawn.ageTracker.AgeBiologicalTicks = _initialAgeTicks + expected;
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();

        if (Pawn.Dead || Props == null)
            return;

        // Simulate rapid aging post-symbiote removal
        long acceleratedAge = (long)(Pawn.ageTracker.AgeBiologicalTicks * Props.removalAgingMultiplier);
        Pawn.ageTracker.AgeBiologicalTicks = acceleratedAge;
    }
}
