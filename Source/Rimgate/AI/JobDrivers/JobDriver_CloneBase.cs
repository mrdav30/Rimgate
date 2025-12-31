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

    protected Building_CloningPod ClonePod => job.targetA.Thing as Building_CloningPod;

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
                var job = CloneJob;
                var pod = ClonePod;
                if (job != pod.CurrentJob)
                    pod.InitiateWork(job, _workToFinish);
            },
            tickAction = () =>
            {
                var pod = ClonePod;
                if (pod.Status == CloningStatus.Idle)
                    pod.SwitchState();
                Pawn actor = pawn;
                float workTick = pod.RemainingWork - StatExtension.GetStatValue(actor, StatDefOf.MedicalTendSpeed, true, -1);
                pod.SetWorkAmount(workTick);
                actor.skills.Learn(SkillDefOf.Medicine, 0.11f, false, false);
                if (pod.RemainingWork > 0) return;
                pod.Refuelable.ConsumeFuel(pod.Refuelable.Fuel);
                Clone();
                pod.SwitchState();
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
                : 1f);
        cloneWork.FailOnDespawnedOrNull(TargetIndex.A);
        cloneWork.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        ToilEffects.WithEffect(cloneWork, EffecterDefOf.Hacking, TargetIndex.A, null);
        yield return cloneWork;
    }

    protected virtual void Clone() { }
}
