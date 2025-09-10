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
    protected Thing StargateToEnter => job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOn(() => !StargateToEnter.TryGetComp<Comp_Stargate>().IsActive);

        yield return Toils_Goto.GotoCell(StargateToEnter.InteractionCell, PathEndMode.OnCell);
        yield return new Toil
        {
            initAction = () =>
            {
                Comp_Stargate gateComp = StargateToEnter.TryGetComp<Comp_Stargate>();
                if (gateComp == null) return;
                pawn.DeSpawn();
                gateComp.AddToSendBuffer(pawn);
            }
        };

        yield break;
    }
}
