using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class CompProperties_ApplyNeed : CompProperties_AbilityEffect
{
    public NeedDef need;

    public FloatRange levelRange;

    public CompProperties_ApplyNeed() => compClass = typeof(CompAbilityEffect_ApplyNeed);
}
