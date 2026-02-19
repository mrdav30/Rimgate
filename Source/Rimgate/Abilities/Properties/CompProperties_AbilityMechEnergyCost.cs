using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompProperties_AbilityMechEnergyCost : CompProperties_AbilityEffect
{
    public float mechEnergyCostPct;

    public bool payCostAtStart = true;

    public CompProperties_AbilityMechEnergyCost() => compClass = typeof(CompAbilityEffect_MechEnergyCost);

    public override IEnumerable<string> ExtraStatSummary()
    {
        yield return string.Concat("RG_AbilityMechEnergyCost".Translate() + ": ", Mathf.RoundToInt(mechEnergyCostPct * 100f).ToString());
    }
}