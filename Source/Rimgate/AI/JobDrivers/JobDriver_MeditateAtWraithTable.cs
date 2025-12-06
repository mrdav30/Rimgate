using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_MeditateAtWraithTable : JobDriver_Meditate
{
    protected const TargetIndex FacingInd = TargetIndex.B;

    private Building Table => TargetA.Thing as Building;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Table, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

        // Decide what cell we face while meditating
        yield return Toils_General.Do(delegate
        {
            // Stand at the interaction cell and face the table itself
            IntVec3 faceCell = Table.Position;
            job.SetTarget(FacingInd, faceCell);
        });

        // Go to the Wraith table's interaction cell
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

        // Main meditation toil
        Toil meditate = ToilMaker.MakeToil("WraithTableMeditate");
        meditate.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
        meditate.defaultCompleteMode = ToilCompleteMode.Delay;
        meditate.defaultDuration = job.def.joyDuration;
        meditate.socialMode = RandomSocialMode.Off;

        if (pawn.HasPsylink && Focus.Thing != null)
        {
            meditate.FailOn(() =>
                Focus.Thing.GetStatValueForPawn(StatDefOf.MeditationFocusStrength, pawn) < float.Epsilon);
        }

        meditate.FailOn(() =>
            !MeditationUtility.CanMeditateNow(pawn)
            || !MeditationUtility.SafeEnvironmentalConditions(pawn, TargetLocA, Map));

        meditate.AddPreTickAction(delegate
        {
            bool onMeditationTime = pawn.GetTimeAssignment() == TimeAssignmentDefOf.Meditate;
            if (job.ignoreJoyTimeAssignment)
            {
                Pawn_PsychicEntropyTracker psychicEntropy = pawn.psychicEntropy;
                bool wasOnMeditationTime = !onMeditationTime && job.wasOnMeditationTimeAssignment;

                if (pawn.IsHashIntervalTick(4000)
                    && psychicEntropy != null
                    && !onMeditationTime
                    && psychicEntropy.CurrentPsyfocus >= Mathf.Max(psychicEntropy.TargetPsyfocus + 0.05f, 0.99f))
                {
                    pawn.jobs.CheckForJobOverride();
                    return;
                }

                if (wasOnMeditationTime && psychicEntropy.TargetPsyfocus < psychicEntropy.CurrentPsyfocus)
                {
                    EndJobWith(JobCondition.InterruptForced);
                    return;
                }

                job.psyfocusTargetLast = psychicEntropy.TargetPsyfocus;
                job.wasOnMeditationTimeAssignment = onMeditationTime;
            }
            else if (pawn.needs.joy?.CurLevelPercentage >= 1f)
            {
                EndJobWith(JobCondition.InterruptForced);
                return;
            }
        });

        meditate.tickAction = delegate
        {
            rotateToFace = FacingInd;
            MeditationTick();
        };

        meditate.AddFinishAction(ApplyWraithMeditationMemories);

        yield return meditate;
    }

    private void ApplyWraithMeditationMemories()
    {
        // Only Wraith / hive-linked pawns get the special thoughts
        if (!pawn.HasHiveConnection())
            return;

        var def = Rand.Chance(0.7f)
            ? RimgateDefOf.Rimgate_WraithCommunedWithHive
            : RimgateDefOf.Rimgate_WraithWhispersFromVoid;

        pawn.TryGiveThought(def);
    }
}
