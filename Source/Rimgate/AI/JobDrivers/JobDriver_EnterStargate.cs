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
    protected Building_Stargate StargateToEnter => job.targetA.Thing as Building_Stargate;

    private Comp_StargateControl GateComp => StargateToEnter?.GateControl;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOn(() => !StargateToEnter.GateControl.IsActive);

        yield return Toils_Goto.GotoCell(StargateToEnter.InteractionCell, PathEndMode.OnCell);
        yield return new Toil
        {
            initAction = () =>
            {
                var traveler = pawn;
                var comp = GateComp;
                if (comp == null) return;
                comp.AddToSendBuffer(traveler);
                traveler.DeSpawn();
            }
        };

        yield break;
    }
}
