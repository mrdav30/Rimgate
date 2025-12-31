using Rimgate;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_PatientGoToSarcophagus : JobDriver
{
    protected Building_Sarcophagus Sarcophagus => (Building_Sarcophagus)job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
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
            return !Sarcophagus.Power.PowerOn
                || Sarcophagus.HasAnyContents
                || !Sarcophagus.Accepts(pawn)
                || !pawn.CanReach(Sarcophagus, PathEndMode.InteractionCell, Danger.Deadly) 
                || !SarcophagusUtil.IsValidForUserType(Sarcophagus, pawn);
        });

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

        Toil wait = Toils_General.Wait(Sarcophagus.OpenTicks, TargetIndex.A);
        wait.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
        wait.WithProgressBarToilDelay(TargetIndex.A);
        yield return wait;

        Toil enter = ToilMaker.MakeToil("EnterSarcophagus");
        enter.initAction = () =>
        {
            var sarcophagus = Sarcophagus;
            var actor = pawn;

            actor.DeSpawn();
            sarcophagus.TryAcceptPawn(actor);
        }
        yield return enter;
    }
}