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
    private const TargetIndex stargateToEnter = TargetIndex.A;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(stargateToEnter);
        this.FailOn(() => !this.job.GetTarget(stargateToEnter).Thing.TryGetComp<Comp_Stargate>().stargateIsActive);

        yield return Toils_Goto.GotoCell(this.job.GetTarget(stargateToEnter).Thing.InteractionCell, PathEndMode.OnCell);
        yield return new Toil
        {
            initAction = () =>
            {
                Comp_Stargate gateComp = this.job.GetTarget(stargateToEnter).Thing.TryGetComp<Comp_Stargate>();
                this.pawn.DeSpawn(DestroyMode.Vanish);
                gateComp.AddToSendBuffer(this.pawn);
            }
        };

        yield break;
    }
}
