using RimWorld;
using Verse;

namespace Rimgate;

public class CompProperties_AnalyzableResearchWhen : CompProperties_CompAnalyzableUnlockResearch
{
    public ResearchProjectDef requiresResearchDef;

    public bool hideWhenDone = true;

    public CompProperties_AnalyzableResearchWhen() => compClass = typeof(CompAnalyzableResearchWhen);
}
