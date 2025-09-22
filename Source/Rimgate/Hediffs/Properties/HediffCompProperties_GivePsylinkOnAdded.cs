using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class HediffCompProperties_GivePsylinkOnAdded : HediffCompProperties
{
    public int minLevel = 1;

    public int maxLevel = 3;

    public IntRange extraPsycasts = new IntRange(1, 2);

    public List<string> abilityTags;

    // optional tag filter (defName or abilityTags)
    public string requiredGene;

    // < 0 to skip
    public float initialPsyfocus = 0.35f;

    public HediffCompProperties_GivePsylinkOnAdded()
    {
        compClass = typeof(HediffComp_GivePsylinkOnAdded);
    }
}