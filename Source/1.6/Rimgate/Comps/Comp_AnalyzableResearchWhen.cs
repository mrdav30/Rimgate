using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class Comp_AnalyzableResearchWhen : CompAnalyzableUnlockResearch
{
    // <= this → full catastrophicFailureChance
    private const int FullRiskLevel = 8;

    // >= this → reduced catastrophic chance
    private const int ReducedRiskLevel = 15;

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

    public override void CompTickRare()
    {
        if (!CanAnalyze || HideInteraction) return;

        base.CompTickRare();
    }

    public override AcceptanceReport CanInteract(Pawn activateBy = null, bool checkOptionalItems = true)
    {
        if (HideInteraction) return false;

        var def = Props?.requiresResearchDef;
        if (def != null && !def.IsFinished)
            return "RG_RequiresResearch".Translate(def.label);

        AcceptanceReport result = base.CanInteract(activateBy, checkOptionalItems);
        if (!result.Accepted)
            return result;

        return true;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (HideInteraction) yield break;

        foreach (var gizmo in base.CompGetGizmosExtra())
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
        // If there is no catastrophic config at all, just use base behavior
        if (Props == null || Props.catastrophicFailureChance <= 0f)
        {
            base.OnAnalyzed(analyzer);
            return;
        }

        float effectiveChance = Props.catastrophicFailureChance;
        if (analyzer != null && analyzer.skills != null)
        {
            // Get Intellectual level (0 if somehow missing)
            int level = analyzer.skills.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;

            // level <= fullRiskLevel → keep full catastrophicFailureChance
            if (level > FullRiskLevel)
            {
                // Map [fullRiskLevel, zeroRiskLevel] to [1, 0]
                float t = (level - FullRiskLevel) / (float)(ReducedRiskLevel - FullRiskLevel);
                if (t > 1f) t = 1f;   // clamp

                float baseChance = Props.catastrophicFailureChance;
                // always at least 5% of base
                float minChance = baseChance * 0.05f;
                effectiveChance = Mathf.Lerp(baseChance, minChance, t);
            }
        }

        // If chance doesn't proc, go normal path
        if (!Rand.Chance(effectiveChance))
        {
            base.OnAnalyzed(analyzer);
            return;
        }

        // --- Catastrophic path ---

        if (!Props.catastrophicBlocksResearch)
        {
            // Still unlocks research but explodes / destroys, etc.
            base.OnAnalyzed(analyzer);
        }
        else
        {
            // reset behavior for further analysis
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
