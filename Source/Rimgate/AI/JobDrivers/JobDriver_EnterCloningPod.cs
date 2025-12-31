using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_EnterCloningPod : JobDriver
{
    private Building_CloningPod _clonePod => (Building_CloningPod)job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed, false);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => !_clonePod.Power.PowerOn
            || !_clonePod.Refuelable.IsFull
            || _clonePod.HasAnyContents
            || !_clonePod.Accepts(pawn));

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell, false);
        Toil toil = Toils_General.Wait(500, TargetIndex.None)
            .FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
        ToilEffects.WithProgressBarToilDelay(toil, TargetIndex.A, false, -0.5f);
        yield return toil;
        Toil enter = new()
        {
            initAction = () =>
            {
                Pawn actor = pawn;
                Building_CloningPod cloningPod = _clonePod;
                Action action = () =>
                {
                    actor.DeSpawn(DestroyMode.Vanish);
                    cloningPod.TryAcceptThing(actor, true);
                };

                if (cloningPod.def.building.isPlayerEjectable
                    || Map.mapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount > 1)
                {
                    action();
                    return;
                }
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        
        yield return enter;
    }
}
