using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompProperties_Dissolves : CompProperties
{
    public int dissolutionAfterDays = 4;

    public float dissolutionFactorIndoors = 0.5f;

    public float dissolutionFactorRain = 2f;

    public bool destroyIfFrozen = false;

    public int amountPerDissolution = 1;

    public string dissolveEveryActionVerb = "dissolves";

    public CompProperties_Dissolves() => compClass = typeof(Comp_Dissolves);
}
