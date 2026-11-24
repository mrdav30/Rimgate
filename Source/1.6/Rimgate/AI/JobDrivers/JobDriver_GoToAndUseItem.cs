using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_GoToAndUseItem : JobDriver_UseItem
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (base.TryMakePreToilReservations(errorOnFailed))
            return pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);

        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
        this.FailOn(() => !base.TargetThingA.TryGetComp<CompUsable>().CanBeUsedBy(pawn));
        this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
        this.FailOnBurningImmobile(TargetIndex.B);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A).FailOnDestroyedNullOrForbidden(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
        yield return Toils_Haul.DropCarriedThing();
        yield return PrepareToUse();
        yield return Use();
    }
}