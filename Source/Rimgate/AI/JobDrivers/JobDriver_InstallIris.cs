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
    private const TargetIndex _irisItem = TargetIndex.A;

    private const TargetIndex _targetStargate = TargetIndex.B;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        job.count = 1;
        Thing stargate = (Thing)job.GetTarget(_targetStargate);
        Thing iris = (Thing)job.GetTarget(_irisItem);
        return pawn.Reserve(stargate, job) 
            && pawn.Reserve(iris, job);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        int useDuration = job.GetTarget(TargetIndex.A).Thing.TryGetComp<CompUsable>().Props.useDuration;
        Thing iris = (Thing)job.GetTarget(_irisItem);

        this.FailOnDestroyedOrNull(_targetStargate);
        this.FailOnDestroyedNullOrForbidden(_irisItem);

        yield return Toils_Goto.GotoThing(_irisItem, PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(_irisItem);
        yield return Toils_Goto.GotoThing(_targetStargate, PathEndMode.Touch);
        Toil toil = Toils_General.Wait(useDuration);
        toil.WithProgressBarToilDelay(_targetStargate);
        toil.WithEffect(base.TargetThingB.def.repairEffect, TargetIndex.B);
        yield return toil;
        yield return new Toil
        {
            initAction = () =>
            {
                Comp_StargateControl gateComp = job.GetTarget(_targetStargate).Thing.TryGetComp<Comp_StargateControl>();
                pawn.carryTracker.innerContainer.Remove(iris);
                iris.Destroy();
                gateComp.HasIris = true;
            }
        };

        yield break;
    }
}
