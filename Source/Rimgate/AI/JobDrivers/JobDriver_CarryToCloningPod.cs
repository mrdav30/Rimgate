using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_CarryToCloningPod : JobDriver
{
    private const int PlacementDelay = 500;

    private Pawn Takee => (Pawn)job.targetA.Thing;

    private Building_CloningPod ClonePod => (Building_CloningPod)job.targetB.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        base.Map.reservationManager.ReleaseAllForTarget(Takee);
        if (pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed))
            return pawn.Reserve(job.targetB, job, errorOnFailed: errorOnFailed);

        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);
        this.FailOnAggroMentalState(TargetIndex.A);
        this.FailOn(() => !ClonePod.Power.PowerOn
            || !ClonePod.Refuelable.IsFull
            || ClonePod.HasAnyContents
            || !ClonePod.Accepts(Takee));

        Toil goToTakee = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell)
            .FailOnDestroyedNullOrForbidden(TargetIndex.A)
            .FailOnDespawnedNullOrForbidden(TargetIndex.B)
            .FailOn(() => ClonePod.HasAnyContents)
            .FailOn(() => !pawn.CanReach(Takee, PathEndMode.OnCell, Danger.Deadly))
            .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
        Toil startCarryingTakee = Toils_Haul.StartCarryThing(TargetIndex.A);
        Toil goToThing = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);

        yield return Toils_Jump.JumpIf(goToThing, () => pawn.IsCarryingPawn(Takee));
        yield return goToTakee;
        yield return startCarryingTakee;
        yield return goToThing;

        Toil wait = Toils_General.Wait(PlacementDelay);
        wait.FailOnCannotTouch(TargetIndex.B, PathEndMode.InteractionCell);
        ToilEffects.WithProgressBarToilDelay(wait, TargetIndex.B);
        yield return wait;

        yield return new Toil()
        {
            initAction = () => {
                var pod = ClonePod;
                var takee = Takee;
                pod.TryAcceptThing(takee);
            }
        };
    }

    public override object[] TaleParameters()
    {
        return new object[2]
        {
            pawn,
            Takee
        };
    }
}
