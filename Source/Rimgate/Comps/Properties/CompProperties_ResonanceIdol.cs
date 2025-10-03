using RimWorld;
using Verse;

namespace Rimgate;

public class CompProperties_ResonanceIdol : CompProperties
{
    public float radius = 16f;

    // keep hediff alive this long; comp refreshes
    public int refreshTicks = 1200;

    // rare spooky ping
    public float negativeThoughtMtbDays = 12f;

    public HediffDef fieldBuffHediff;

    public HediffDef hostBoostHediff;

    public ThoughtDef thoughtNear;

    public ThoughtDef thoughtNearForWraith;

    public ThoughtDef thoughtNegative;

    public CompProperties_ResonanceIdol()
    {
        compClass = typeof(Comp_ResonanceIdol);
    }
}