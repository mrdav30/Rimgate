using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_InsertSymbioteQueen : JobDriver
{
    private const TargetIndex QueenInd = TargetIndex.A;

    private const TargetIndex PoolInd = TargetIndex.B;

    private Thing Queen => job.GetTarget(QueenInd).Thing;

    private Building_SymbioteSpawningPool Pool => job.GetTarget(PoolInd).Thing as Building_SymbioteSpawningPool;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.GetTarget(QueenInd), job, 1, -1, null, errorOnFailed) 
            && pawn.Reserve(job.GetTarget(PoolInd), job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(QueenInd);
        this.FailOnDestroyedOrNull(PoolInd);

        yield return Toils_Goto.GotoThing(QueenInd, PathEndMode.ClosestTouch);

        job.count = 1; 

        var startCarry = Toils_Haul.StartCarryThing(QueenInd);
        startCarry.FailOn(() => Queen == null || Queen.Destroyed);
        yield return startCarry;

        yield return Toils_Goto.GotoThing(PoolInd, PathEndMode.InteractionCell);

        Toil insert = new Toil();
        insert.initAction = () =>
        {
            Pawn actor = insert.actor;
            var pool = Pool;
            if (pool == null || pool.Destroyed)
            {
                actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                return;
            }

            if (pool == null || pool.HasQueen)
            {
                actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                return;
            }

            var carried = actor.carryTracker.CarriedThing;
            if (carried == null || carried.def != pool.SymbiotePool?.Props.symbioteQueenDef)
            {
                actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                return;
            }

            // Try to insert into pool
            if (!pool.TryAcceptQueen(carried, canMerge: false))
            {
                // Could not insert, drop on ground
                actor.carryTracker.TryDropCarriedThing(pool.Position, ThingPlaceMode.Near, out _);
                actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                return;
            }

            // Success: the queen should now be inside the pool's innerContainer
            actor.jobs.EndCurrentJob(JobCondition.Succeeded);
        };
        insert.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return insert;
    }
}
