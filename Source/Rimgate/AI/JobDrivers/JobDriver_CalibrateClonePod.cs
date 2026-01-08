using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_CalibrateClonePod : JobDriver
{
    protected Building_CloningPod ClonePod => job.targetA.Thing as Building_CloningPod;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOn(() => !ClonePod.Powered || !ClonePod.HasHostPawn);

        Toil goToThing = Toils_Goto.GotoCell(ClonePod.InteractionCell, PathEndMode.OnCell)
            .FailOnDespawnedOrNull(TargetIndex.A);
        yield return goToThing;

        Toil calibrationWork = new Toil()
        {
            initAction = () =>
            {
                var pod = ClonePod;
                if (ClonePod.Status != CloningStatus.Idle || ClonePod.CloningType == CloneType.None)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                pod.InitiateCloningProcess(RimgateModSettings.BaseCalibrationTicks);
            },
            tickAction = () =>
            {
                var pod = ClonePod;
                Pawn actor = pawn;
                pod.TickCalibrationWork(actor);
                if (pod.Status == CloningStatus.CalibrationFinished || pod.RemainingCalibrationWork <= 0)
                    ReadyForNextToil();
            },
            defaultCompleteMode = ToilCompleteMode.Never
        };
        calibrationWork.FailOn(() => !ClonePod.Powered || !ClonePod.HasHostPawn);
        ToilEffects.WithProgressBar(
            calibrationWork,
            TargetIndex.A,
            () =>
            {
                float total = RimgateModSettings.BaseCalibrationTicks;
                float done = total - ClonePod.RemainingCalibrationWork;
                return Mathf.Clamp01(done / total);
            });
        calibrationWork.FailOnDespawnedOrNull(TargetIndex.A);
        calibrationWork.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        ToilEffects.WithEffect(calibrationWork, EffecterDefOf.Hacking, TargetIndex.A, null);
        yield return calibrationWork;
    }
}
