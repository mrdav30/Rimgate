using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompProperties_AbilityEssenceCost : CompProperties_AbilityEffect
{
    public float essenceCost;

    public CompProperties_AbilityEssenceCost() => compClass = typeof(CompAbilityEffect_EssenceCost);

    public override IEnumerable<string> ExtraStatSummary()
    {
        yield return string.Concat("RG_AbilityEssenceCost".Translate() + ": ", Mathf.RoundToInt(essenceCost * 100f).ToString());
    }
}
