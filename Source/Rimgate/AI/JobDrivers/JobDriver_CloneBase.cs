using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public abstract class JobDriver_CloneBase : JobDriver
{
    protected float _workToFinish = 4500f;

    protected abstract CloneType CloneJob { get; }

    protected Building_WraithCloningPod ClonePod => (Building_WraithCloningPod)job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOn(() => !ClonePod.Power.PowerOn || !ClonePod.Refuelable.IsFull || !ClonePod.HasAnyContents);

        Toil goToThing = Toils_Goto.GotoCell(ClonePod.InteractionCell, PathEndMode.OnCell)
            .FailOnDespawnedOrNull(TargetIndex.A);
        yield return goToThing;

        Toil cloneWork = new Toil()
        {
            initAction = () =>
            {
                if (CloneJob != ClonePod.CurrentJob)
                    ClonePod.InitiateWork(CloneJob, _workToFinish);
            },
            tickAction = () =>
            {
                if (ClonePod.Status == CloningStatus.Idle)
                    ClonePod.SwitchState();
                Pawn actor = pawn;
                float workTick = ClonePod.RemainingWork - StatExtension.GetStatValue(actor, StatDefOf.MedicalTendSpeed, true, -1);
                ClonePod.SetWorkAmount(workTick);
                actor.skills.Learn(SkillDefOf.Medicine, 0.11f, false, false);
                if (ClonePod.RemainingWork > 0) return;
                ClonePod.Refuelable.ConsumeFuel(ClonePod.Refuelable.Fuel);
                Clone();
                ClonePod.SwitchState();
                ReadyForNextToil();
            },
            defaultCompleteMode = ToilCompleteMode.Never
        };
        cloneWork.FailOn(() => !ClonePod.Power.PowerOn || !ClonePod.Refuelable.IsFull || !ClonePod.HasAnyContents);
        ToilEffects.WithProgressBar(
            cloneWork,
            TargetIndex.A,
            () => ClonePod.RemainingWork > 0
                ? (_workToFinish - ClonePod.RemainingWork) / _workToFinish
                : 1f,
            false,
            -0.5f,
            false);
        cloneWork.FailOnDespawnedOrNull(TargetIndex.A);
        cloneWork.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        ToilEffects.WithEffect(cloneWork, EffecterDefOf.Hacking, TargetIndex.A, null);
        yield return cloneWork;
    }

    protected virtual void Clone() { }
}
