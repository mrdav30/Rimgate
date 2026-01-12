using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_ToggleIris : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => Map.designationManager.DesignationOn(job.targetA.Thing, RimgateDefOf.Rimgate_DesignationToggleIris) == null);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.Wait(15).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        Toil finalize = ToilMaker.MakeToil("MakeNewToils");
        finalize.initAction = delegate
        {
            Pawn actor = finalize.actor;
            var t = actor.CurJob.targetA.Thing;
            var didSomething = false;
            if (t is Building_DHD dhd)
            {
                dhd.DoToggleIrisRemote();
                didSomething = true;

            }

            if (!didSomething && t is Building_Stargate gate && gate.WantsIrisToggled)
            {
                gate.DoToggleIris();
                didSomething = true;
            }

            if (didSomething)
                base.Map.designationManager.DesignationOn(t, RimgateDefOf.Rimgate_DesignationToggleIris)?.Delete();
            return;
        };
        finalize.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return finalize;
    }
}
