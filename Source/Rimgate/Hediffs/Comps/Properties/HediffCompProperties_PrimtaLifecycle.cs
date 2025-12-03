using UnityEngine;
using Verse;

namespace Rimgate;

public class HediffCompProperties_PrimtaLifecycle : HediffCompProperties
{
    // 1.25 ~ 2.5 years
    public IntRange ticksToMature = new IntRange(4500000, 9000000);

    // extra 5 days before crisis
    public IntRange ticksAfterMatureGrace = new IntRange(900000, 1200000);

    // chance prim'ta takes over instead of both dying
    public float takeoverChance = 0.35f;

    public HediffCompProperties_PrimtaLifecycle() => compClass = typeof(HediffComp_PrimtaLifecycle);
}
