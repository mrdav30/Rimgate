using UnityEngine;
using Verse;

namespace Rimgate;

public class HediffCompProperties_Regeneration : HediffCompProperties
{
    public HediffDef regeneratingHediff;

    public int checkIntervalTicks = 1200;

    public bool canResurrect;
    public int resurrectionAttempts = 99;
    public IntRange resurrectionDelayRange = new IntRange(1600, 2500);
    public string resurrectionMessageKey;
    public bool restoreMechWeaponOnDeath;

    public float regenerateMissingTimeFactor = 1;

    public bool healInjuries;
    // 0..1 per interval when scars exist
    public float healInjuryChance = 0.25f;
    public float injuryRegeneration = 30000;

    public bool healPermanentInjuries;
    // scars heal much faster
    public float regeneratePermanentTimeFactor = 0.75f;

    public bool healChronics;
    // slower than scars by default
    public float healChronicChance = 0.35f;
    // optional: regen-tissue time scaling
    public float regenerateChronicTimeFactor = 1f;

    public bool showHealingFleck = true;
    public int healingFleckInterval = 2400;

    public HediffCompProperties_Regeneration()
    {
        compClass = typeof(HediffComp_Regeneration);
    }

    public override void ResolveReferences(HediffDef parent)
    {
        base.ResolveReferences(parent);
        checkIntervalTicks = Mathf.Max(0, checkIntervalTicks);
        resurrectionAttempts = Mathf.Max(0, resurrectionAttempts);
        healInjuryChance = Mathf.Clamp01(healInjuryChance);
        healChronicChance = Mathf.Clamp01(healChronicChance);
    }
}
