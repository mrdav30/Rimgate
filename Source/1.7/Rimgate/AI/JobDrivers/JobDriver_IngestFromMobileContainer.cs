using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_IngestFromMobileContainer : JobDriver
{
    private const TargetIndex FoodInd = TargetIndex.A;
    private const TargetIndex ContainerInd = TargetIndex.B;
    private const TargetIndex TableCellInd = TargetIndex.C;

    private Thing Food => job.GetTarget(FoodInd).Thing;

    private Building_MobileContainer Container => job.GetTarget(ContainerInd).Thing as Building_MobileContainer;

    private float ChewDurationMultiplier
    {
        get
        {
            Thing food = Food;
            if (food?.def?.ingestible != null && !food.def.ingestible.useEatingSpeedStat)
                return 1f;

            return 1f / pawn.GetStatValue(StatDefOf.EatingSpeed);
        }
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (Container == null || Food == null || !Container.AllowColonistsUseContents)
            return false;

        if (!pawn.CanReach(Container, PathEndMode.Touch, pawn.NormalMaxDanger()))
            return false;

        int maxAmountToPickup = FoodUtility.GetMaxAmountToPickup(Food, pawn, job.count);
        if (maxAmountToPickup <= 0)
            return false;

        if (!pawn.Reserve(Food, job, 10, maxAmountToPickup, null, errorOnFailed))
            return false;

        job.count = maxAmountToPickup;
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedNullOrForbidden(ContainerInd);
        this.FailOn(() =>
        {
            Thing food = Food;
            Building_MobileContainer container = Container;
            if (container == null || food == null || food.Destroyed || !container.AllowColonistsUseContents)
                return true;

            return food.ParentHolder != container
                && pawn.carryTracker?.CarriedThing != food;
        });

        yield return Toils_Goto.GotoThing(ContainerInd, PathEndMode.Touch);

        yield return TakeFoodFromContainer();

        if (!pawn.Drafted)
        {
            yield return Toils_Ingest.CarryIngestibleToChewSpot(pawn, FoodInd)
                .FailOnDestroyedOrNull(FoodInd);
            yield return Toils_Ingest.FindAdjacentEatSurface(TableCellInd, FoodInd);
        }

        yield return Toils_Ingest.ChewIngestible(pawn, ChewDurationMultiplier, FoodInd, TableCellInd)
            .FailOn((Toil x) =>
            {
                Thing food = Food;
                return food == null
                    || food.Destroyed
                    || (!food.Spawned && pawn.carryTracker?.CarriedThing != food);
            })
            .FailOnCannotTouch(FoodInd, PathEndMode.Touch);

        yield return Toils_Ingest.FinalizeIngest(pawn, FoodInd);
    }

    private Toil TakeFoodFromContainer()
    {
        Toil toil = ToilMaker.MakeToil("TakeFoodFromMobileContainer");
        toil.initAction = delegate
        {
            Thing food = Food;
            Building_MobileContainer container = Container;

            if (container == null
                || food == null
                || food.Destroyed
                || !container.AllowColonistsUseContents
                || food.ParentHolder != container)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            int requested = Mathf.Max(1, job.count);
            int toTake = Mathf.Min(requested, food.stackCount);
            int taken = pawn.carryTracker.TryStartCarry(food, toTake, reserve: false);
            if (taken <= 0 || pawn.carryTracker.CarriedThing == null)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            job.targetA = pawn.carryTracker.CarriedThing;
            job.count = taken;
        };

        toil.defaultCompleteMode = ToilCompleteMode.Instant;
        return toil;
    }
}
