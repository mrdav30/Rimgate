using Rimgate;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_PatientGoToSarcophagus : JobDriver
{
    protected Building_Bed_Sarcophagus Sarcophagus => (Building_Bed_Sarcophagus)job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Sarcophagus, job, 1, -1, null, errorOnFailed);
    }

    public override bool CanBeginNowWhileLyingDown()
    {
        return JobInBedUtility.InBedOrRestSpotNow(pawn, job.GetTarget(TargetIndex.A));
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnBurningImmobile(TargetIndex.A);

        // Fail if sarchophagus has lost power,
        // is no longer unreachable,
        // or is no longer the right user type  for the patient
        this.FailOn(delegate
        {
            return !Sarcophagus.PowerComp.PowerOn
                || Sarcophagus.HasAnyContents
                || !Sarcophagus.Accepts(pawn)
                || !pawn.CanReach(Sarcophagus, PathEndMode.Touch, Danger.Deadly) 
                || !SarcophagusRestUtility.IsValidBedForUserType(Sarcophagus, pawn);
        });

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        Toil wait = Toils_General.Wait(Sarcophagus.OpenTicks, TargetIndex.A);
        wait.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        wait.WithProgressBarToilDelay(TargetIndex.A);
        yield return wait;

        Toil enter = ToilMaker.MakeToil("EnterSarcophagus");
        enter.initAction = () =>
        {
            var sarcophagus = Sarcophagus;
            var actor = pawn;

            ((Entity)actor).DeSpawn((DestroyMode)0);
            sarcophagus.TryAcceptPawn(actor);
        };
        enter.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return enter;
    }
}