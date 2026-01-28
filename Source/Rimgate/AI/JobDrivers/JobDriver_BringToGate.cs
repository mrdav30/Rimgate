using System;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class JobDriver_BringToGate : JobDriver
{
    private Thing ThingToHaul => job.targetA.Thing;

    private Building_Gate Gate => job.targetB.Thing as Building_Gate;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        Thing thing = job.targetA.Thing;
        job.count = thing?.stackCount ?? 0;

        if(thing is Pawn takee)
            base.Map.reservationManager.ReleaseAllForTarget(takee);

        return pawn.Reserve(job.targetA, job, 1, thing.stackCount, errorOnFailed: errorOnFailed) 
            && pawn.Reserve(job.targetB, job, 1, errorOnFailed: errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.B);
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => !(Gate.IsActive == true));

        if (ThingToHaul as Pawn != null)
            this.FailOnMobile(TargetIndex.A);

        yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        yield return Toils_Goto.GotoCell(Gate.InteractionCell, PathEndMode.OnCell);
        yield return new Toil
        {
            initAction = () =>
            {
                Thing hauled = ThingToHaul;
                Building_Gate gate = Gate;
                pawn.carryTracker.innerContainer.Remove(hauled);
                gate.AddToSendBuffer(hauled);
                if(hauled is Pawn hauledPawn)
                    RimgateEvents.Notify_ColonistEnteredGate(hauledPawn, gate);
            }
        };

        yield break;
    }
}
