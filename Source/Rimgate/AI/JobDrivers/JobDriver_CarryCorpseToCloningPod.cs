using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_CarryCorpseToCloningPod : JobDriver
{
    private Corpse _corpse => (Corpse)job.GetTarget(TargetIndex.A).Thing;

    private Building_CloningPod _clonePod => (Building_CloningPod)job.GetTarget(TargetIndex.B).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (pawn.Reserve(_corpse, job, 1, -1, null, errorOnFailed))
            return pawn.Reserve(_clonePod, job, 1, 0, null, errorOnFailed);

        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);
        this.FailOnAggroMentalState(TargetIndex.A);
        this.FailOn(() => !_clonePod.Accepts(_corpse));

        Toil goToTakee = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell)
            .FailOnDestroyedNullOrForbidden(TargetIndex.A)
            .FailOnDespawnedNullOrForbidden(TargetIndex.B)
            .FailOn(() => _clonePod.HasAnyContents)
            .FailOn(() => !pawn.CanReach(_corpse, PathEndMode.OnCell, Danger.Deadly))
            .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
        Toil startCarryingTakee = Toils_Haul.StartCarryThing(TargetIndex.A);
        Toil goToThing = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);

        yield return Toils_Jump.JumpIf(goToThing, () => pawn.IsCarryingThing(_corpse));
        yield return goToTakee;
        yield return startCarryingTakee;
        yield return goToThing;

        Toil wait = Toils_General.Wait(500, TargetIndex.None);
        wait.FailOnCannotTouch(TargetIndex.B, PathEndMode.InteractionCell);
        wait.WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
        yield return wait;

        Toil putInto = ToilMaker.MakeToil("PutIntoCloningPod");
        putInto.initAction = () =>
        {
            _clonePod.TryAcceptThing(_corpse);
        };
        putInto.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return putInto;
    }

    public override object[] TaleParameters()
    {
        return new object[2]
        {
            pawn,
            _corpse.InnerPawn
        };
    }
}
