using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using RimWorld;
using UnityEngine;

namespace Rimgate;

public class JobDriver_CarryToSarcophagus : JobDriver
{
    protected Pawn Patient => (Pawn)job.GetTarget(TargetIndex.A).Thing;

    protected Building_Bed_Sarcophagus Sarcophagus => (Building_Bed_Sarcophagus)job.GetTarget(TargetIndex.B).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        Patient.ClearAllReservations();
        if (pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed))
        {
            return pawn.Reserve(Sarcophagus, job, 1, 0, null, errorOnFailed);
        }

        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);
        this.FailOnAggroMentalState(TargetIndex.A);
        this.FailOn(() => !Sarcophagus.Accepts(Patient));
        Toil goToTakee = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell)
            .FailOnDestroyedNullOrForbidden(TargetIndex.A)
            .FailOnDespawnedNullOrForbidden(TargetIndex.B)
            .FailOn(() => Sarcophagus.HasAnyContents)
            .FailOn(() => !pawn.CanReach(Patient, PathEndMode.OnCell, Danger.Deadly))
            //    .FailOn(() => job.def == JobDefOf.Arrest && !Patient.CanBeArrestedBy(pawn))
            //    .FailOn(() => (job.def == JobDefOf.Rescue || job.def == JobDefOf.Capture) && !Patient.Downed)
            .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
        Toil startCarryingTakee = Toils_Haul.StartCarryThing(TargetIndex.A);
        //Toil startCarrying = Toils_Haul.StartCarryThing(TargetIndex.A)
        //    .FailOn(() => !SarcophagusRestUtility.IsValidSarcophagusFor(
        //        BedSarcophagus,
        //        Patient,
        //        pawn,
        //        Patient.guest.GuestStatus));
        //startCarrying.AddPreInitAction(CheckMakeTakeeGuest);

        Toil goToThing = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
        yield return Toils_Jump.JumpIf(goToThing, () => pawn.IsCarryingPawn(Patient));
        yield return goToTakee;
        yield return startCarryingTakee;
        yield return goToThing;
        Toil wait = Toils_General.Wait(Sarcophagus.OpenTicks, TargetIndex.B);
        wait.FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
        wait.WithProgressBarToilDelay(TargetIndex.B);
        yield return wait;

        Toil putInto = ToilMaker.MakeToil("PutIntoSarcophagus");
        putInto.initAction = () => 
        {
            var sarcophagus = Sarcophagus;
            var taker = pawn;
            var takee = Patient;

            SarcophagusRestUtility.PutIntoSarcophagus(sarcophagus, taker, takee, true);
        };
        putInto.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return putInto;
    }

    private void CheckMakeTakeeGuest()
    {
        Log.Warning("checking making patient guest");
        if (!job.def.makeTargetPrisoner
            && Patient.Faction != Faction.OfPlayer
            && Patient.HostFaction != Faction.OfPlayer
            && Patient.guest != null
            && !Patient.IsWildMan()
            && Patient.DevelopmentalStage != DevelopmentalStage.Baby)
        {
            Log.Warning("making patient guest");
            Patient.guest.SetGuestStatus(Faction.OfPlayer);
            QuestUtility.SendQuestTargetSignals(Patient.questTags, "Rescued", Patient.Named("SUBJECT"));
        }
    }
}
