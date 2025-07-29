using Verse;

namespace Rimgate;

public class HediffComp_GoauldSymbiote : HediffComp
{
    public HediffCompProperties_GoauldSymbiote Props => (HediffCompProperties_GoauldSymbiote)props;

    private bool initialized = false;
    private long initialAgeTicks;

    // Cache once to avoid boxing allocations
    private Pawn Pawn => parent.pawn;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);

        if (Pawn.Dead || Props == null)
            return;

        if (!initialized)
        {
            initialAgeTicks = Pawn.ageTracker.AgeBiologicalTicks;
            initialized = true;
        }

        // Apply anti-aging once per year (default RimWorld year: 3600000 ticks)
        if (Find.TickManager.TicksGame % 3600000 == 0)
            ApplyAgeReduction();
    }

    private void ApplyAgeReduction()
    {
        // Reduce biological age growth by applying a lifespan factor
        long current = Pawn.ageTracker.AgeBiologicalTicks;
        long expected = (long)((current - initialAgeTicks) / Props.lifespanFactor);
        Pawn.ageTracker.AgeBiologicalTicks = initialAgeTicks + expected;
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
