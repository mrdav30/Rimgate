using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_InsertZpmIntoHousing : JobDriver
{
    private Building_ZPM _zpm => job.targetA.Thing as Building_ZPM;

    private Building_ZPMHousing _housing => job.targetB.Thing as Building_ZPMHousing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)
            && pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);
        this.FailOn(() => _housing == null || !_housing.CanAcceptZpm);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

        // Pick up the ZPM
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);

        // Go to housing interaction cell
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);

        // Insert into housing
        var insert = new Toil
        {
            initAction = () =>
            {
                var carrier = pawn;
                var housing = _housing;
                if (housing == null || !housing.CanAcceptZpm)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                var carried = carrier.carryTracker.CarriedThing as Building_ZPM;
                if (carried == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Remove from carry tracker container first
                carrier.carryTracker.innerContainer.Remove(carried);

                if (!housing.TryInsertZpm(carried))
                {
                    // Failed to insert; drop ZPM nearby so it isn't lost
                    GenPlace.TryPlaceThing(carried, carrier.Position, carrier.Map, ThingPlaceMode.Near);
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                carrier.carryTracker.DestroyCarriedThing();
                EndJobWith(JobCondition.Succeeded);
            }
        };

        yield return insert;
    }
}
