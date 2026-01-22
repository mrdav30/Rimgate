using System;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class JobDriver_EnterGate : JobDriver
{
    protected Building_Gate GateToEnter => job.targetA.Thing as Building_Gate;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOn(() => !GateToEnter.IsActive);

        yield return Toils_Goto.GotoCell(GateToEnter.InteractionCell, PathEndMode.OnCell);
        yield return new Toil
        {
            initAction = () =>
            {
                var traveler = pawn;
                var gate = GateToEnter;
                gate.AddToSendBuffer(traveler);
                traveler.DeSpawn();
            }
        };

        yield break;
    }
}
