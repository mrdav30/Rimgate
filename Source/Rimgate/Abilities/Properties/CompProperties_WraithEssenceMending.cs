using RimWorld;

namespace Rimgate;

public class CompProperties_WraithEssenceMending : CompProperties_AbilityEffect
{
    public bool healsChronic = true;

    public int totalChronicToHeal = 1;

    public CompProperties_WraithEssenceMending() => compClass = typeof(CompAbilityEffect_WraithEssenceMending);
}
