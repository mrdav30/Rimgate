using System;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class JobDriver_DialStargate : JobDriver
{
    private Thing _dhd => job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        Comp_DHDControl dhdComp = _dhd.TryGetComp<Comp_DHDControl>();
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOn(() => dhdComp.GetLinkedStargate().IsActive);

        yield return Toils_Goto.GotoCell(_dhd.InteractionCell, PathEndMode.OnCell);
        yield return new Toil
        {
            initAction = () =>
            {
                Comp_StargateControl linkedStargate = dhdComp.GetLinkedStargate();
                linkedStargate.QueueOpen(dhdComp.LastDialledAddress, 200);
            }
        };

        yield break;
    }
}
