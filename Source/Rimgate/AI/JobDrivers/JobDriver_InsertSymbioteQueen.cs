using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_InsertSymbioteQueen : JobDriver
{
    private Thing Queen => job.targetA.Thing;

    private Building_SymbioteSpawningPool Pool => job.targetB.Thing as Building_SymbioteSpawningPool;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) 
            && pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

        job.count = 1; 

        var startCarry = Toils_Haul.StartCarryThing(TargetIndex.A);
        startCarry.FailOn(() => Queen == null || Queen.Destroyed);
        yield return startCarry;

        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);

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
        yield return insert;
    }
}
