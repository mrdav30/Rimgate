using RimWorld;
using Verse;

namespace Rimgate;

public class CompProperties_UseEffectAddGene : CompProperties_UseEffect
{
    public GeneDef geneDef;

    public CompProperties_UseEffectAddGene() => compClass = typeof(CompUseEffect_AddGene);
}