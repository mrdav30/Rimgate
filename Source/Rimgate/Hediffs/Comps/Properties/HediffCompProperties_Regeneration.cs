using RimWorld;
using Verse;

namespace Rimgate;

public class HediffCompProperties_Regeneration : HediffCompProperties
{
    public HediffDef regeneratingHediff;

    public int checkIntervalTicks = 2500;

    public bool healScars;
    // 0..1 per interval when scars exist
    public float healScarChance = 0.25f; 
    // scars heal much faster
    public float healScarTimeFactor = 0.75f;

    public bool healChronics;
    // slower than scars by default
    public float healChronicChance = 0.35f;
    // optional: regen-tissue time scaling
    public float healChronicTimeFactor = 1f;

    public bool showHealingFleck = true;

    public HediffCompProperties_Regeneration()
    {
        compClass = typeof(HediffComp_Regeneration);
    }
}
