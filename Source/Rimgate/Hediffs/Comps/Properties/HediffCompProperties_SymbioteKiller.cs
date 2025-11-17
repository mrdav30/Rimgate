using UnityEngine;
using Verse;

namespace Rimgate;

public class HediffCompProperties_SymbioteKiller : HediffCompProperties
{
    public float killThreshold = 0.7f;

    public float killChance = 0.35f;

    public HediffCompProperties_SymbioteKiller() => compClass = typeof(HediffComp_SymbioteKiller);
}
