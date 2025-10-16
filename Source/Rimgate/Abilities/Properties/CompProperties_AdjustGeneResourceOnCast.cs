using Rimgate;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompProperties_AdjustGeneResourceOnCast : CompProperties_AbilityEffect
{
    public GeneDef geneDef;

    public float amount = 0.05f;       // +5% by default

    public string affects = "Caster";  // "Caster" or "Target"

    public bool onlyIfTargetHarmed = false;

    public bool allowFreeDraw = false;

    public CompProperties_AdjustGeneResourceOnCast()
        => compClass = typeof(CompAbilityEffect_AdjustGeneResourceOnCast);
}