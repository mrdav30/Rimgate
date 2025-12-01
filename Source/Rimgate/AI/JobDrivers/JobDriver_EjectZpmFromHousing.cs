using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_EjectZpmFromHousing : JobDriver
{
    private const TargetIndex HousingInd = TargetIndex.A;

    private Building_ZPMHousing _housing => job.GetTarget(HousingInd).Thing as Building_ZPMHousing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.GetTarget(HousingInd), job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(HousingInd);
        this.FailOn(() => _housing == null || !_housing.HasAnyZpm);

        yield return Toils_Goto.GotoThing(HousingInd, PathEndMode.InteractionCell);

        var eject = new Toil
        {
            initAction = () =>
            {
                var housing = _housing;
                if (housing == null || !housing.HasAnyZpm)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int toEject = Math.Max(1, job.count);

                for (int i = 0; i < toEject; i++)
                {
                    if (!housing.HasAnyZpm)
                        break;

                    var dropCell = Utils.BestDropCellNearThing(housing);
                    if (!housing.TryEjectOneZpm(dropCell))
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                }

                EndJobWith(JobCondition.Succeeded);
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };

        yield return eject;
    }
}
