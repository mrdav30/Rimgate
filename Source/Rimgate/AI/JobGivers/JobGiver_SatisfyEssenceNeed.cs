using RimWorld;
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

        if (geneEssence.FilledPodsAllowed)
        {
            Building_WraithCocoonPod pod = Building_WraithCocoonPod.FindFilledPodFor(pawn);
            if (pod != null)
            {
                var job = JobMaker.MakeJob(RimgateDefOf.Rimgate_DrainLifeFromCocoonedPrisoner, pod);
                job.ability = abilityDrain;
                return job;
            }
        }

        if (!geneEssence.PrisonersAllowed)
            return null;

        Map map = pawn.MapHeld;
        if (map == null)
            return null;

        if (abilityDrain.def.jobDef == null)
            return null;

        if (GenClosest.ClosestThingReachable(
            pawn.PositionHeld,
            map,
            ThingRequest.ForUndefined(),
            PathEndMode.Touch,
            TraverseParms.For(pawn),
            9999f,
            validator: thing =>
            {
                if (thing is not Pawn candidate
                    || !candidate.IsPrisonerOfColony
                    || !pawn.CanReserve(candidate))
                {
                    return false;
                }

                return abilityDrain.CanApplyOn(new LocalTargetInfo(candidate));
            },
            customGlobalSearchSet: map.mapPawns.PrisonersOfColonySpawned) is Pawn prisoner)
            return abilityDrain.GetJob(new LocalTargetInfo(prisoner), LocalTargetInfo.Invalid);

        return null;
    }
}
