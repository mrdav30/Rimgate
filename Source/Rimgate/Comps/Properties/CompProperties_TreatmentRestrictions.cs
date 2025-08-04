using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class CompProperties_TreatmentRestrictions : CompProperties
{
  public List<HediffDef> alwaysTreatableHediffs;

  public List<HediffDef> neverTreatableHediffs;

  public List<HediffDef> nonCriticalTreatableHediffs;

  public List<TraitDef> alwaysTreatableTraits;

  public List<HediffDef> usageBlockingHediffs;

  public List<TraitDef> usageBlockingTraits;

  public List<string> disallowedRaces;

  [MayRequireBiotech]
  public List<XenotypeDef> disallowedXenotypes;

  public CompProperties_TreatmentRestrictions() => compClass = typeof(Comp_TreatmentRestrictions);
}
