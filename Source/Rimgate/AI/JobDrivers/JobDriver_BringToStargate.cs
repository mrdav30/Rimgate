using System;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class JobDriver_BringToStargate : JobDriver
{
    private const TargetIndex _thingToHaul = TargetIndex.A;

    private const TargetIndex _targetStargate = TargetIndex.B;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        Thing thing = (Thing)job.GetTarget(_thingToHaul);
        job.count = thing.stackCount;
        return pawn.Reserve(thing, job, 1, thing.stackCount) 
            && pawn.Reserve(thing, job, 1, thing.stackCount);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        Thing thing = (Thing)job.GetTarget(_thingToHaul);

        this.FailOnDestroyedOrNull(_targetStargate);
        this.FailOnDestroyedNullOrForbidden(_thingToHaul);
        this.FailOn(() => 
            !job.GetTarget(_targetStargate).Thing.TryGetComp<Comp_StargateControl>().IsActive);

        if (thing as Pawn != null)
            this.FailOnMobile(_thingToHaul);

        yield return Toils_Goto.GotoCell(_thingToHaul, PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(_thingToHaul);
        yield return Toils_Goto.GotoCell(job.GetTarget(_targetStargate).Thing.InteractionCell, PathEndMode.OnCell);
        yield return new Toil
        {
            initAction = () =>
            {
                Comp_StargateControl gateComp = job.GetTarget(_targetStargate).Thing.TryGetComp<Comp_StargateControl>();
                pawn.carryTracker.innerContainer.Remove(thing);
                gateComp.AddToSendBuffer(thing);
            }
        };

        yield break;
    }
}
