using System;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class JobDriver_InstallIris : JobDriver
{
    private Thing _iris => (Thing)job.targetA.Thing;

    private Building_Stargate _gate => (Building_Stargate)job.targetB.Thing;

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
                Comp_StargateControl gateComp = gate?.GateControl;
                if (gateComp == null || gateComp.HasIris)
                    return;
                user.carryTracker.innerContainer.Remove(iris);
                iris.Destroy();
                gateComp.HasIris = true;
            }
        };

        yield break;
    }
}
