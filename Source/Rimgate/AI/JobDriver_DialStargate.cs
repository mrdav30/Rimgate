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
    private const TargetIndex _targetDHD = TargetIndex.A;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.GetTarget(_targetDHD), job);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        Comp_DialHomeDevice dhdComp = job.GetTarget(_targetDHD).Thing.TryGetComp<Comp_DialHomeDevice>();
        this.FailOnDestroyedOrNull(_targetDHD);
        this.FailOn(() => dhdComp.GetLinkedStargate().StargateIsActive);

        yield return Toils_Goto.GotoCell(job.GetTarget(_targetDHD).Thing.InteractionCell, PathEndMode.OnCell);
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
