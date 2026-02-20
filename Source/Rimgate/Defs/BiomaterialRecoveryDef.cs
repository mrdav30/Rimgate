using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimgate;

public class BiomaterialRecoveryDef : Def
{
    [NoTranslate]
    private string uiIconPath;

    // What we remove from the corpse/pawn.
    public HediffDef removesHediff;

    // Optional: if null, we’ll use removesHediff.spawnThingOnRemoved.
    public ThingDef spawnThingOverride;

    // Gating / balance
    public ResearchProjectDef researchPrerequisite;
    public ThingDef requiredKit;

    public float workAmount = 2000; // work units, like recipes
    public StatDef workSpeedStat;

    // Skill requirements (optional; 0 = none)
    public List<SkillRequirement> skillRequirements;

    public SkillDef workSkill;
    public float workSkillLearnFactor = 1f;
    public EffecterDef effectWorking;
    public SoundDef soundWorking;

    public float successChance = 1f;
    public StatDef successChanceStat;

    // Filters
    public bool allowRotten = false;
    public bool animalsOnly = true;

    public ThingDef RecoverableThing => spawnThingOverride ?? removesHediff?.spawnThingOnRemoved;

    [Unsaved(false)]
    private Texture2D _cachedIcon;

    public Texture2D UIIcon
    {
        get
        {
            if (!uiIconPath.NullOrEmpty())
                _cachedIcon ??= ContentFinder<Texture2D>.Get(uiIconPath);

            return _cachedIcon;
        }
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        workSpeedStat ??= RimgateDefOf.MedicalOperationSpeed;
        workSkill ??= SkillDefOf.Medicine;
        effectWorking ??= EffecterDefOf.Surgery;
        soundWorking ??= SoundDefOf.Recipe_Surgery;
    }

    public bool PawnSatisfiesSkillRequirements(Pawn pawn, out string reason)
    {
        reason = null;
        if (skillRequirements == null)
            return true;

        if (pawn?.skills == null)
        {
            reason = "no skills";
            return false;
        }

        for (int i = 0; i < skillRequirements.Count; i++)
        {
            var req = skillRequirements[i];
            if (!req.PawnSatisfies(pawn))
            {
                reason = $"{req.skill.LabelCap} too low - need {req.minLevel}";
                return false;
            }
        }

        return true;
    }

    public int GetWorkTicks(Pawn actor)
    {
        StatDef stat = workSpeedStat ?? RimgateDefOf.MedicalOperationSpeed;
        float speed = actor.GetStatValue(stat, true);
        if (speed <= 0.01f) speed = 0.01f;
        return (int)(workAmount / speed);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        if (workSkill != null)
        {
            yield return new StatDrawEntry(StatCategoryDefOf.Basics,
                "Skill".Translate(),
                workSkill.LabelCap,
                "Stat_Recipe_Skill_Desc".Translate(),
                4404);
        }

        if (requiredKit != null)
        {
            yield return new StatDrawEntry(StatCategoryDefOf.Basics,
                "Ingredients".Translate(),
                $"{requiredKit.label}",
                "Stat_Recipe_Ingredients_Desc".Translate(),
                4405,
                null,
                Dialog_InfoCard.DefsToHyperlinks(Gen.YieldSingle(requiredKit)));
        }

        if (researchPrerequisite != null)
        {
            yield return new StatDrawEntry(StatCategoryDefOf.Basics,
                "RG_ResearchRequirements".Translate(),
                researchPrerequisite.label,
                "RG_Stat_ResearchRequirements_Desc".Translate(),
                4403);
        }

        if (!skillRequirements.NullOrEmpty())
        {
            yield return new StatDrawEntry(StatCategoryDefOf.Basics,
                "SkillRequirements".Translate(),
                skillRequirements.Select((SkillRequirement sr) => sr.Summary).ToCommaList(),
                "Stat_Recipe_SkillRequirements_Desc".Translate(),
                4403);
        }

        var product = spawnThingOverride ?? removesHediff.spawnThingOnRemoved;

        if (product != null)
        {
            yield return new StatDrawEntry(StatCategoryDefOf.Basics,
                "Products".Translate(),
                product.label,
                "Stat_Recipe_Products_Desc".Translate(),
                4405,
                null,
                Dialog_InfoCard.DefsToHyperlinks(Gen.YieldSingle(product)));

            if (workAmount > 0f)
            {
                yield return new StatDrawEntry(StatCategoryDefOf.Basics,
                    StatDefOf.WorkToMake.LabelCap,
                    workAmount.ToStringWorkAmount(),
                    StatDefOf.WorkToMake.description,
                    StatDefOf.WorkToMake.displayPriorityInCategory);
            }
        }

        if (workSpeedStat != null)
        {
            yield return new StatDrawEntry(StatCategoryDefOf.Basics,
                "WorkSpeedStat".Translate(),
                workSpeedStat.LabelCap,
                "Stat_Recipe_WorkSpeedStat_Desc".Translate(),
                4402);
        }

        yield return new StatDrawEntry(StatCategoryDefOf.Surgery,
            "SurgerySuccessChanceFactor".Translate(),
            successChance.ToStringPercent(),
            "Stat_Thing_Surgery_SuccessChanceFactor_Desc".Translate(),
            4102);
    }
}
