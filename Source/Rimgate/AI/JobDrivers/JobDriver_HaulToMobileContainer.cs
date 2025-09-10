using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_HaulToMobileContainer : JobDriver_HaulToContainer
{
    public int initialCount;

    public Comp_MobileContainer Mobile => base.Container?.TryGetComp<Comp_MobileContainer>();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref initialCount, "initialCount", 0);
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        // Reserve the container; queues are already handled
        if (Container != null)
            pawn.Reserve(Container, job, 1, -1, null, errorOnFailed);
        pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job);
        pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
        return true;
    }

    public override void Notify_Starting()
    {
        base.Notify_Starting();
        var comp = Mobile;

        // If targetA wasn't prefilled, select one of the chosen items
        var tc = job.targetA.IsValid
            ? new ThingCount(job.targetA.Thing, job.targetA.Thing.stackCount)
            : MobileContainerUtility.FindThingToLoad(pawn, comp);

        if (tc.Thing == null || tc.Count <= 0) 
        { 
            EndJobWith(JobCondition.Incompletable);
            return;
        }

        bool isCarrying = job.playerForced 
            && pawn.carryTracker.CarriedThing != null 
            && pawn.carryTracker.CarriedThing != tc.Thing;

        if (isCarrying)
            pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);

        // Clamp to what's actually still needed for *this exact thing*
        int remaining = comp.RemainingToLoadFor(tc.Thing);
        if (remaining <= 0) 
        { 
            EndJobWith(JobCondition.Incompletable);
            return;
        }

        job.targetA = tc.Thing;
        job.count = Mathf.Min(tc.Count, remaining);
        initialCount = tc.Count;

        pawn.Reserve(tc.Thing, job);
    }
}