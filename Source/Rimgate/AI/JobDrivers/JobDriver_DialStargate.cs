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
    private Thing _dhd => job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(_dhd, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        Comp_DialHomeDevice dhdComp = _dhd.TryGetComp<Comp_DialHomeDevice>();
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOn(() => dhdComp.GetLinkedStargate().IsActive);

        yield return Toils_Goto.GotoCell(_dhd.InteractionCell, PathEndMode.OnCell);
        yield return new Toil
        {
            initAction = () =>
            {
                Comp_Stargate linkedStargate = dhdComp.GetLinkedStargate();
                linkedStargate.OpenStargateDelayed(dhdComp.LastDialledAddress, 200);
            }
        };

        yield break;
    }
}
