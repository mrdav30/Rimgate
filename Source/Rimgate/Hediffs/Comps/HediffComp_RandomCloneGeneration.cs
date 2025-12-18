using RimWorld;
using System;
using System.Linq;
using Verse;

namespace Rimgate;

public class HediffComp_RandomCloneGeneration : HediffComp
{
    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        if (parent is not Hediff_Clone clone) return;
        clone.CloneGeneration = new Random().Next(0, 9999);
    }
}
