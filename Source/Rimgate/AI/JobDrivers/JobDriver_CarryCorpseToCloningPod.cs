using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_CarryCorpseToCloningPod : JobDriver
{
    private Corpse Corpse => (Corpse)job.targetA.Thing;

    private Building_CloningPod ClonePod => (Building_CloningPod)job.targetB.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed))
            return pawn.Reserve(job.targetB, job, errorOnFailed: errorOnFailed);

        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);
        this.FailOnAggroMentalState(TargetIndex.A);
        this.FailOn(() => !ClonePod.Accepts(Corpse));

        Toil goToTakee = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell)
            .FailOnDestroyedNullOrForbidden(TargetIndex.A)
            .FailOnDespawnedNullOrForbidden(TargetIndex.B)
            .FailOn(() => ClonePod.HasAnyContents)
            .FailOn(() => !pawn.CanReach(Corpse, PathEndMode.OnCell, Danger.Deadly))
            .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
        Toil startCarryingTakee = Toils_Haul.StartCarryThing(TargetIndex.A);
        Toil goToThing = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);

        yield return Toils_Jump.JumpIf(goToThing, () => pawn.IsCarryingThing(Corpse));
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
            var pod = ClonePod;
            var corpse = Corpse;
            pod.TryAcceptThing(corpse);
        };
        yield return putInto;
    }

    public override object[] TaleParameters()
    {
        return new object[2]
        {
            pawn,
            Corpse.InnerPawn
        };
    }
}
