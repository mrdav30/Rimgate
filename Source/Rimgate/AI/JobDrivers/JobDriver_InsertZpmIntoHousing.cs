using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_InsertZpmIntoHousing : JobDriver
{
    private const TargetIndex ZpmInd = TargetIndex.A;
    private const TargetIndex HousingInd = TargetIndex.B;

    private Building_ZPM _zpm => job.GetTarget(ZpmInd).Thing as Building_ZPM;
    private Building_ZPMHousing _housing => job.GetTarget(HousingInd).Thing as Building_ZPMHousing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.GetTarget(ZpmInd), job, 1, -1, null, errorOnFailed)
            && pawn.Reserve(job.GetTarget(HousingInd), job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedNullOrForbidden(ZpmInd);
        this.FailOnDestroyedOrNull(HousingInd);
        this.FailOn(() => _housing == null || !_housing.CanAcceptZpm);

        yield return Toils_Goto.GotoThing(ZpmInd, PathEndMode.ClosestTouch);

        // Pick up the ZPM
        yield return Toils_Haul.StartCarryThing(ZpmInd);

        // Go to housing interaction cell
        yield return Toils_Goto.GotoThing(HousingInd, PathEndMode.InteractionCell);

        // Insert into housing
        var insert = new Toil
        {
            initAction = () =>
            {
                var housing = _housing;
                if (housing == null || !housing.CanAcceptZpm)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                var carried = pawn.carryTracker.CarriedThing as Building_ZPM;
                if (carried == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Remove from carry tracker container first
                pawn.carryTracker.innerContainer.Remove(carried);

                if (!housing.TryInsertZpm(carried))
                {
                    // Failed to insert; drop ZPM nearby so it isn't lost
                    GenPlace.TryPlaceThing(carried, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                pawn.carryTracker.DestroyCarriedThing(); // TODO: make sure it's not dangling
                EndJobWith(JobCondition.Succeeded);
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };

        yield return insert;
    }
}
