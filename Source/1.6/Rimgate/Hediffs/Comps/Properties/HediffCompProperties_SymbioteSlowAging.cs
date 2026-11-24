using Verse;

namespace Rimgate;

public class HediffCompProperties_SymbioteSlowAging : HediffCompProperties
{
    // 4x slower aging while symbiote is active
    public float lifespanFactor = 4.0f;

    // 2.5x age penalty on removal
    public float removalAgingMultiplier = 2.5f;

    public HediffCompProperties_SymbioteSlowAging() => compClass = typeof(HediffComp_SymbioteSlowAging);
}
