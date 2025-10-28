using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class CompAnalyzableResearchWhen : CompAnalyzableUnlockResearch
{
    public new CompProperties_AnalyzableResearchWhen Props => (CompProperties_AnalyzableResearchWhen)props;

    public override bool HideInteraction => Props?.hideWhenDone == true && Find.AnalysisManager.IsAnalysisComplete(AnalysisID);

    public bool CanAnalyze
    {
        get
        {
            var def = Props?.requiresResearchDef;
            if (def != null)
                return def.IsFinished;
            return true;
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!CanAnalyze || HideInteraction) yield break;

        foreach(var gizmo in base.CompGetGizmosExtra())
            yield return gizmo;
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (!CanAnalyze || HideInteraction) yield break;

        foreach (var menu in base.CompFloatMenuOptions(selPawn))
            yield return menu;
    }
}
