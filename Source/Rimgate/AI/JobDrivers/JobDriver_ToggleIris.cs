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
        this.FailOn(() => base.Map.designationManager.DesignationOn(base.TargetThingA, RimgateDefOf.Rimgate_DesignationToggleIris) == null);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.Wait(15).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        Toil finalize = ToilMaker.MakeToil("MakeNewToils");
        finalize.initAction = delegate
        {
            Pawn actor = finalize.actor;
            ThingWithComps thingWithComps = (ThingWithComps)actor.CurJob.targetA.Thing;
            for (int i = 0; i < thingWithComps.AllComps.Count; i++)
            {
                if (thingWithComps.AllComps[i] is Comp_StargateControl stargate && stargate.WantsIrisToggled)
                {
                    stargate.DoToggleIris();
                    break;
                }

                if (thingWithComps.AllComps[i] is Comp_DHDControl dhd && dhd.WantsIrisToggled)
                {
                    dhd.DoToggleIrisRemote();
                    break;
                }
            }

            base.Map.designationManager.DesignationOn(thingWithComps, RimgateDefOf.Rimgate_DesignationToggleIris)?.Delete();
        };
        finalize.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return finalize;
    }
}
