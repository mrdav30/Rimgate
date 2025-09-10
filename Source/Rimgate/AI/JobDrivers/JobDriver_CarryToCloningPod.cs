using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_CarryToCloningPod : JobDriver
{
    private Pawn _takee => (Pawn)job.GetTarget(TargetIndex.A).Thing;

    private Building_WraithCloningPod _clonePod => (Building_WraithCloningPod)job.GetTarget(TargetIndex.B).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (pawn.Reserve(_takee, job, 1, -1, null, errorOnFailed))
            return pawn.Reserve(_clonePod, job, 1, -1, null, errorOnFailed);

        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);
        this.FailOnAggroMentalState(TargetIndex.A);
        this.FailOn(() => !_clonePod.Power.PowerOn
            || !_clonePod.Refuelable.IsFull
            || _clonePod.HasAnyContents
            || !_clonePod.Accepts(_takee));

        Toil goToTakee = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell)
            .FailOnDestroyedNullOrForbidden(TargetIndex.A)
            .FailOnDespawnedNullOrForbidden(TargetIndex.B)
            .FailOn(() => _clonePod.HasAnyContents)
            .FailOn(() => !pawn.CanReach(_takee, PathEndMode.OnCell, Danger.Deadly))
            .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
        Toil startCarryingTakee = Toils_Haul.StartCarryThing(TargetIndex.A);
        Toil goToThing = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);

        yield return Toils_Jump.JumpIf(goToThing, () => pawn.IsCarryingPawn(_takee));
        yield return goToTakee;
        yield return startCarryingTakee;
        yield return goToThing;

        Toil wait = Toils_General.Wait(500, TargetIndex.None);
        wait.FailOnCannotTouch(TargetIndex.B, PathEndMode.InteractionCell);
        ToilEffects.WithProgressBarToilDelay(wait, TargetIndex.B);
        yield return wait;

        yield return new Toil()
        {
            initAction = () => _clonePod.TryAcceptThing(_takee),
            defaultCompleteMode = ToilCompleteMode.Instant
        };
    }

    public override object[] TaleParameters()
    {
        return new object[2]
        {
            pawn,
            _takee
        };
    }
}
