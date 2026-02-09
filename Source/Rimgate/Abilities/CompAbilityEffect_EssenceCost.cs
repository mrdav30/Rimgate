using Rimgate;
using RimWorld;
using Verse;

namespace Rimgate;

public class CompAbilityEffect_EssenceCost : CompAbilityEffect
{
    public new CompProperties_AbilityEssenceCost Props => (CompProperties_AbilityEssenceCost)props;

    private bool HasEnoughEssence
    {
        get
        {
            Gene_WraithEssenceMetabolism gene = parent.pawn.GetActiveGene<Gene_WraithEssenceMetabolism>();
            if (gene == null || gene.Value < Props.essenceCost)
                return false;

            return true;
        }
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        if (Props.payCostAtStart)
            BiologyUtil.OffsetEssenceCost(parent.pawn, 0f - Props.essenceCost);
    }

    public override bool GizmoDisabled(out string reason)
    {
        Gene_WraithEssenceMetabolism gene = parent.pawn.GetActiveGene<Gene_WraithEssenceMetabolism>();
        if (gene == null)
        {
            reason = "RG_AbilityDisabledNoEssenceGene".Translate(parent.pawn);
            return true;
        }

        if (gene.Value < Props.essenceCost)
        {
            reason = "RG_AbilityDisabledNoEssence".Translate(parent.pawn);
            return true;
        }

        float num = TotalEssenceCostOfQueuedAbilities();
        float num2 = Props.essenceCost + num;
        if (Props.essenceCost > float.Epsilon && num2 > gene.Value)
        {
            reason = "RG_AbilityDisabledNoEssence".Translate(parent.pawn);
            return true;
        }

        reason = null;
        return false;
    }

    public override bool AICanTargetNow(LocalTargetInfo target) => HasEnoughEssence;

    private float TotalEssenceCostOfQueuedAbilities()
    {
        float num = !(parent.pawn.jobs?.curJob?.verbToUse is Verb_CastAbility verb_CastAbility)
            ? 0f
            : verb_CastAbility.ability?.HemogenCost() ?? 0f;
        if (parent.pawn.jobs == null)
            return num;

        for (int i = 0; i < parent.pawn.jobs.jobQueue.Count; i++)
        {
            if (parent.pawn.jobs.jobQueue[i].job.verbToUse is Verb_CastAbility verb_CastAbility2)
                num += verb_CastAbility2.ability?.HemogenCost() ?? 0f;
        }

        return num;
    }
}