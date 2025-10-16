using Rimgate;
using RimWorld;
using UnityEngine;

namespace Rimgate;

public class CompProperties_AdjustNeedOnCast : CompProperties_AbilityEffect
{
    public string affects = "Caster"; // "Caster" or "Target"
    
    public NeedDef needDef;
    
    public float amount = 0f;
    
    public bool onlyIfTargetHarmed = false;

    public CompProperties_AdjustNeedOnCast()
        => compClass = typeof(CompAbilityEffect_AdjustNeedOnCast);
}