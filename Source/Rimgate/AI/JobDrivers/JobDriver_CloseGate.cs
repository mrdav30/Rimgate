using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_CloseGate : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => base.Map.designationManager.DesignationOn(base.TargetThingA, RimgateDefOf.Rimgate_DesignationCloseGate) == null);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.Wait(15).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        Toil finalize = ToilMaker.MakeToil("MakeNewToils");
        finalize.initAction = delegate
        {
            Pawn actor = finalize.actor;

            if(actor.CurJob.targetA.Thing is Building_DHD dhd && dhd.WantsGateClosed)
            {
                dhd.DoCloseGate();
                Map.designationManager.DesignationOn(dhd, RimgateDefOf.Rimgate_DesignationCloseGate)?.Delete();
            }
        };
        finalize.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return finalize;
    }
}
