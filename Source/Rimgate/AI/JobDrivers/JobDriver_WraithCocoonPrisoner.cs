using RimWorld;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_WraithCocoonPrisoner : JobDriver
{
    private const int CocoonWorkTicks = 500;

    private Pawn Victim => (Pawn)job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        Map.reservationManager.ReleaseAllForTarget(Victim);
        return pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => !Victim.IsPrisonerOfColony); // only valid if still a prisoner

        // Go to the prisoner
        Toil goToPrisoner = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        goToPrisoner.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        goToPrisoner.FailOnSomeonePhysicallyInteracting(TargetIndex.A);

        Toil carryVictim = Toils_Haul.StartCarryThing(TargetIndex.A, reserve: true);

        // Cocooning "cast" with progress bar
        Toil cocoonWork = Toils_General.Wait(CocoonWorkTicks);
        cocoonWork.WithProgressBar(
            TargetIndex.None,
            () => 1f - ((float)cocoonWork.actor.jobs.curDriver.ticksLeftThisToil / CocoonWorkTicks));
        cocoonWork.WithEffect(() => EffecterDefOf.Surgery, () => pawn, null);
        cocoonWork.PlaySustainerOrSound(RimgateDefOf.Rimgate_WraithCocoonCast);

        yield return goToPrisoner;
        yield return carryVictim; // Carry so they don't wander away
        yield return cocoonWork;

        // Finalize: spawn pod & insert prisoner
        Toil finish = new Toil
        {
            initAction = () =>
            {
                Pawn actor = pawn;
                Pawn victim = Victim;

                Map map = actor.Map;
                IntVec3 cell = Utils.BestDropCellNearThing(actor);

                // Spawn cocoon pod at the victim's cell
                var pod = ThingMaker.MakeThing(RimgateDefOf.Rimgate_WraithCocoonPod) as Building_WraithCocoonPod;
                pod.IsAbilitySpawn = true;
                GenSpawn.Spawn(pod, cell, map, Rot4.North, WipeMode.Vanish);
                pod.SetFaction(Faction.OfPlayer);

                // Stuff pawn into the pod
                if (!pod.TryAcceptThing(victim, allowSpecialEffects: true))
                {
                    Log.Warning($"Rimgate :: Wraith cocoon pod at {cell} failed to accept {victim}.");
                    pod.Destroy(); // clean up orphan pod
                    return;
                }

                MoteMaker.ThrowText(cell.ToVector3Shifted(),
                    map,
                    "RG_WraithCocoonTrap_Cocooned".Translate(victim.Named("PAWN")),
                    3.5f);
            }
        };

        yield return finish;
    }
}
