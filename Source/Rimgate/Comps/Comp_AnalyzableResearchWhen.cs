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

    public override void OnAnalyzed(Pawn analyzer)
    {
        if (Props == null 
            || Props.catastrophicFailureChance <= 0f 
            || !Rand.Chance(Props.catastrophicFailureChance))
        {
            // Normal path – let the base comp unlock research as usual
            base.OnAnalyzed(analyzer);
            return;
        }

        // Catastrophic path:
        // no (or blocked) research unlock, object may die/explode
        if (!Props.catastrophicBlocksResearch)
        {
            // still unlocks research but explodes anyway
            base.OnAnalyzed(analyzer);
        }
        else
        {
            // remove and re-add so this instance can’t be reused
            Find.AnalysisManager.RemoveAnalysisDetails(AnalysisID);
            Find.AnalysisManager.AddAnalysisTask(AnalysisID, Props.analysisRequiredRange.TrueMax);
        }

        // Optional letter
        if (!Props.catastrophicLetterLabelKey.NullOrEmpty() &&
            !Props.catastrophicLetterTextKey.NullOrEmpty())
        {
            Find.LetterStack.ReceiveLetter(
                Props.catastrophicLetterLabelKey.Translate(),
                Props.catastrophicLetterTextKey.Translate(parent.LabelCap),
                Props.catastrophicLetterDef ?? LetterDefOf.NegativeEvent,
                parent
            );
        }

        // Optional explosion
        if (Props.catastrophicExplosion && parent.MapHeld != null)
        {
            var map = parent.MapHeld;
            var dmgDef = Props.catastrophicExplosionDamageDef ?? DamageDefOf.Bomb;
            GenExplosion.DoExplosion(
                parent.PositionHeld,
                map,
                Props.catastrophicExplosionRadius,
                dmgDef,
                instigator: analyzer,
                damAmount: Props.catastrophicExplosionDamage,
                armorPenetration: -1f,
                explosionSound: null
            );
        }

        // Destroy the object if configured
        if (Props.catastrophicDestroysThing && !parent.Destroyed)
            parent.Destroy(DestroyMode.KillFinalize);
    }
}
