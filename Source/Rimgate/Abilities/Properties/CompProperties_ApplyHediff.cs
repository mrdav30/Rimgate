using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimgate;

public class CompProperties_ApplyHediff : CompProperties_AbilityEffect
{
    public HediffDef hediffToReceive;

    public HediffDef hediffToGive;

    public int durationTime = 0;

    public List<StatModifier> durationTimeStatFactors = new List<StatModifier>();

    public CompProperties_ApplyHediff() => compClass = typeof(CompAbilityEffect_ApplyHediffs);
}
