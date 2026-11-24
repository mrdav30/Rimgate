using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_InstallIris : JobDriver
{
    private Thing _iris => (Thing)job.targetA.Thing;

    private Building_Gate _gate => (Building_Gate)job.targetB.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        job.count = 1;
        return pawn.Reserve(job.targetA, job)
            && pawn.Reserve(job.targetB, job);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        int useDuration = _iris.TryGetComp<CompUsable>()?.Props.useDuration ?? 0;

        this.FailOnDestroyedOrNull(TargetIndex.B);
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
        Toil toil = Toils_General.Wait(useDuration);
        toil.WithProgressBarToilDelay(TargetIndex.B);
        toil.WithEffect(base.TargetThingB.def.repairEffect, TargetIndex.B);
        yield return toil;
        yield return new Toil
        {
            initAction = () =>
            {
                var user = pawn;
                var gate = _gate;
                var iris = _iris;
                if (gate.HasIris)
                    return;
                user.carryTracker.innerContainer.Remove(iris);
                iris.Destroy();
                gate.HasIris = true;
            }
        };

        yield break;
    }
}
