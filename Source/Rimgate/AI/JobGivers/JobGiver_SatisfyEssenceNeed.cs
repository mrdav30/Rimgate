using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobGiver_SatisfyEssenceNeed : ThinkNode_JobGiver
{
    public override float GetPriority(Pawn pawn)
    {
        if (!ModsConfig.BiotechActive)
            return 0f;

        if (pawn.GetActiveGene<Gene_WraithEssenceMetabolism>() == null)
            return 0f;

        return 9.1f;
    }

    protected override Job TryGiveJob(Pawn pawn)
    {
        if (!ModsConfig.BiotechActive)
            return null;

        Gene_WraithEssenceMetabolism geneEssence = pawn.GetActiveGene<Gene_WraithEssenceMetabolism>();
        if (geneEssence == null)
            return null;

        if (!geneEssence.ShouldConsumeResourceNow())
            return null;

        Ability abilityDrain = pawn.abilities.GetAbility(RimgateDefOf.Rimgate_WraithLifeDrainAbility);
        if (abilityDrain == null || !abilityDrain.CanCast)
            return null;

        if (!geneEssence.FilledPodsAllowed) 
            return null;

        Building_WraithCocoonPod pod = Building_WraithCocoonPod.FindFilledPodFor(pawn);
        if (pod != null)
        {
            var job = JobMaker.MakeJob(RimgateDefOf.Rimgate_DrainLifeFromCocoonedPrisoner, pod);
            job.ability = abilityDrain;
            return job;
        }

        return null;
    }
}