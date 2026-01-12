using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_DialStargate : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        var dhd = job.targetA.Thing as Building_DHD;
        var gate = dhd?.LinkedStargate;
        if (gate == null)
        {
            EndJobWith(JobCondition.Incompletable);
            yield break;
        }

        PlanetTile tile = dhd.LastDialledAddress;
        if (!tile.Valid)
        {
            EndJobWith(JobCondition.Incompletable);
            yield break;
        }

        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOn(() => gate == null || gate.IsActive);

        yield return Toils_Goto.GotoCell(job.targetA.Thing.InteractionCell, PathEndMode.OnCell);
        yield return new Toil
        {
            initAction = () =>
            {
                gate.QueueOpen(tile, 200);
            }
        };

        yield break;
    }
}
