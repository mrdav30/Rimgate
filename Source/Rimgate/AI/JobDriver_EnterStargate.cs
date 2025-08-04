using System;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class JobDriver_EnterStargate : JobDriver
{
    private const TargetIndex _stargateToEnter = TargetIndex.A;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(_stargateToEnter);
        this.FailOn(() => !job.GetTarget(_stargateToEnter).Thing.TryGetComp<Comp_Stargate>().StargateIsActive);

        yield return Toils_Goto.GotoCell(job.GetTarget(_stargateToEnter).Thing.InteractionCell, PathEndMode.OnCell);
        yield return new Toil
        {
            initAction = () =>
            {
                Comp_Stargate gateComp = job.GetTarget(_stargateToEnter).Thing.TryGetComp<Comp_Stargate>();
                pawn.DeSpawn(DestroyMode.Vanish);
                gateComp.AddToSendBuffer(pawn);
            }
        };

        yield break;
    }
}
