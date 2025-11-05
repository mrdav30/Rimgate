using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_MeditateOnGoauldThrone : JobDriver_Meditate
{
    protected const TargetIndex FacingInd = TargetIndex.B;

    private Building Throne => TargetA.Thing as Building;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Throne, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

        // Decide what cell we face while meditating
        yield return Toils_General.Do(delegate
        {
            // Face "in front" of the throne
            IntVec3 faceCell = Throne.InteractionCell + Throne.Rotation.FacingCell;
            job.SetTarget(FacingInd, faceCell);
        });

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

        // Main meditation toil
        Toil meditate = ToilMaker.MakeToil("GoauldThroneMeditate");
        meditate.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
        meditate.defaultCompleteMode = ToilCompleteMode.Delay;
        meditate.defaultDuration = job.def.joyDuration;
        meditate.tickAction = delegate
        {
            if (!MeditationUtility.CanMeditateNow(pawn) ||
                !MeditationUtility.SafeEnvironmentalConditions(pawn, TargetLocA, Map))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            rotateToFace = FacingInd;
            MeditationTick();
        };

        yield return meditate;
    }
}
