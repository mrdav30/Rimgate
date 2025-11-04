using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class Comp_AnalyzableResearchWhen : CompAnalyzableUnlockResearch
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

    public override void CompTick()
    {
        if (!CanAnalyze || HideInteraction) return;

        base.CompTick();
    }

    public override AcceptanceReport CanInteract(Pawn activateBy = null, bool checkOptionalItems = true)
    {
        AcceptanceReport result = base.CanInteract(activateBy, checkOptionalItems);
        if (!result.Accepted)
            return result;

        if (HideInteraction) return false;

        var def = Props?.requiresResearchDef;
        if (def != null && !def.IsFinished)
            return "RG_RequiresResearch".Translate(def.label);

        return true;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (HideInteraction) yield break;

        foreach(var gizmo in base.CompGetGizmosExtra())
            yield return gizmo;
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (HideInteraction) yield break;

        foreach (var menu in base.CompFloatMenuOptions(selPawn))
            yield return menu;
    }

    public override string CompInspectStringExtra()
    {
        return HideInteraction 
            ? string.Empty 
            : base.CompInspectStringExtra();
    }
}
