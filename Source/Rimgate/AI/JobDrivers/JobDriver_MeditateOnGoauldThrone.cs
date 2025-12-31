using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_MeditateOnGoauldThrone : JobDriver_Meditate
{
    private Building Throne => job.targetA.Thing as Building;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

        // Decide what cell we face while meditating
        yield return Toils_General.Do(delegate
        {
            // Face "in front" of the throne (it only faces south)
            IntVec3 faceCell = Throne.InteractionCell + Rot4.South.FacingCell;
            job.SetTarget(TargetIndex.B, faceCell);
        });

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

        // Main meditation toil
        Toil meditate = ToilMaker.MakeToil("GoauldThroneMeditate");
        meditate.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
        meditate.defaultCompleteMode = ToilCompleteMode.Delay;
        meditate.defaultDuration = job.def.joyDuration;
        meditate.socialMode = RandomSocialMode.Off;

        if (pawn.HasPsylink && Focus.Thing != null)
        {
            meditate.FailOn(() => Focus.Thing.GetStatValueForPawn(StatDefOf.MeditationFocusStrength, pawn) < float.Epsilon);
        }

        meditate.FailOn(() => !MeditationUtility.CanMeditateNow(pawn) || !MeditationUtility.SafeEnvironmentalConditions(pawn, base.TargetLocA, base.Map));

        meditate.AddPreTickAction(delegate
        {
            bool flag = pawn.GetTimeAssignment() == TimeAssignmentDefOf.Meditate;
            if (job.ignoreJoyTimeAssignment)
            {
                Pawn_PsychicEntropyTracker psychicEntropy = pawn.psychicEntropy;
                bool flag2 = !flag && job.wasOnMeditationTimeAssignment;
                if (pawn.IsHashIntervalTick(4000) 
                    && psychicEntropy != null 
                    && !flag 
                    && psychicEntropy.CurrentPsyfocus >= Mathf.Max(psychicEntropy.TargetPsyfocus + 0.05f, 0.99f))
                {
                    pawn.jobs.CheckForJobOverride();
                    return;
                }

                if (flag2 && psychicEntropy.TargetPsyfocus < psychicEntropy.CurrentPsyfocus)
                {
                    EndJobWith(JobCondition.InterruptForced);
                    return;
                }

                job.psyfocusTargetLast = psychicEntropy.TargetPsyfocus;
                job.wasOnMeditationTimeAssignment = flag;
            }
            else if (pawn.needs.joy.CurLevelPercentage >= 1f)
            {
                EndJobWith(JobCondition.InterruptForced);
                return;
            }    
        });
        meditate.tickAction = delegate
        {
            rotateToFace = TargetIndex.B;
            MeditationTick();
        };

        meditate.AddFinishAction(() =>
        {
            if (!pawn.HasSymbiote())
                return;

            pawn.TryGiveThought(RimgateDefOf.Rimgate_GoauldThroneCravingDominion);
        });

        yield return meditate;
    }
}
