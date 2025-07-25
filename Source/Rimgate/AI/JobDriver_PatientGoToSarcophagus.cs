using Rimgate;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_PatientGoToSarcophagus : JobDriver
{
    private const TargetIndex SarcophagusInd = TargetIndex.A;

    protected Building_Bed_Sarcophagus BedSarcophagus => (Building_Bed_Sarcophagus)job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(BedSarcophagus, job, 1, -1, null, errorOnFailed);
    }

    public override bool CanBeginNowWhileLyingDown()
    {
        return JobInBedUtility.InBedOrRestSpotNow(pawn, job.GetTarget(SarcophagusInd));
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(SarcophagusInd);
        this.FailOnBurningImmobile(SarcophagusInd);
        this.FailOn(delegate
        {
            // Fail if MedPod has lost power, is no longer unreachable, or is no longer the right user type (colonist / guest / slave / prisoner) for the patient
            return !BedSarcophagus.powerComp.PowerOn || !pawn.CanReach(BedSarcophagus, PathEndMode.OnCell, Danger.Deadly) || !SarcophagusRestUtility.IsValidBedForUserType(BedSarcophagus, pawn);
        });

        yield return Toils_General.DoAtomic(delegate
        {
            job.count = 1;
        });
        yield return Toils_Bed.GotoBed(SarcophagusInd);
        yield return Toils_LayDown.LayDown(SarcophagusInd, true, false);
    }
}