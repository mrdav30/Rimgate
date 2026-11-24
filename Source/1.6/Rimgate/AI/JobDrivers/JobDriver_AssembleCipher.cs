using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_AssembleCipher : JobDriver_UseItem
{
    private Thing CipherFragment => job.targetA.Thing;

    private CompUseEffect_AssembleAndStartQuest AssembleComp => CipherFragment?.TryGetComp<CompUseEffect_AssembleAndStartQuest>();

    private int RequiredCount => AssembleComp?.Props?.requiredCount ?? 0;

    private float NearbySearchRadius => AssembleComp?.Props?.nearbySearchRadius ?? 0f;

    private string FragmentLabel => CipherFragment?.LabelShort ?? "cipher fragment";

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => AssembleComp == null || RequiredCount <= 0);
        this.FailOn(() => TargetThingA == null || !TargetThingA.TryGetComp<CompUsable>().CanBeUsedBy(pawn));

        Toil dropUnrelatedCarry = ToilMaker.MakeToil("AssembleCipher_DropUnrelatedCarry");
        dropUnrelatedCarry.initAction = () =>
        {
            Thing anchor = CipherFragment;
            if (anchor == null || anchor.Destroyed)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            Thing carried = pawn.carryTracker?.CarriedThing;
            if (carried != null && carried.def != anchor.def)
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
        };
        yield return dropUnrelatedCarry;

        Toil ensureColonyHasEnough = ToilMaker.MakeToil("AssembleCipher_EnsureColonyHasEnough");
        ensureColonyHasEnough.initAction = () =>
        {
            Thing anchor = CipherFragment;
            if (anchor == null || anchor.Destroyed)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            int total = TreasureCipherUtility.CountColonyReachableFragments(pawn, anchor);
            if (total >= RequiredCount)
                return;

            Messages.Message(
                "RG_CannotDecode".Translate("RG_CannotDecode_Count".Translate(RequiredCount, FragmentLabel, total)),
                MessageTypeDefOf.RejectInput,
                historical: false);
            EndJobWith(JobCondition.Incompletable);
        };
        yield return ensureColonyHasEnough;

        Toil findNextRemoteSource = ToilMaker.MakeToil("AssembleCipher_FindNextRemoteSource");
        findNextRemoteSource.initAction = () =>
        {
            Thing anchor = CipherFragment;
            if (anchor == null || anchor.Destroyed)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            int localCount = TreasureCipherUtility.CountLocalFragments(pawn, anchor, NearbySearchRadius);
            if (localCount >= RequiredCount)
                return;

            if (!TreasureCipherUtility.TryFindClosestRemoteFragment(pawn, anchor, NearbySearchRadius, out Thing source))
            {
                int total = TreasureCipherUtility.CountColonyReachableFragments(pawn, anchor);
                Messages.Message(
                    "RG_CannotDecode".Translate("RG_CannotDecode_Count".Translate(RequiredCount, FragmentLabel, total)),
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            int countToCarry = Math.Min(RequiredCount - localCount, source.stackCount);
            int reservable = pawn.Map?.reservationManager?.CanReserveStack(pawn, source) ?? 0;
            countToCarry = Math.Min(countToCarry, reservable);
            if (countToCarry <= 0)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (!pawn.Reserve(source, job, 1, countToCarry, null, false))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            job.SetTarget(TargetIndex.B, source);
            job.count = countToCarry;
        };
        yield return findNextRemoteSource;

        Toil goToAssemblePoint = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
            .FailOnDestroyedNullOrForbidden(TargetIndex.A);

        yield return Toils_Jump.JumpIf(
            goToAssemblePoint,
            () => TreasureCipherUtility.CountLocalFragments(pawn, CipherFragment, NearbySearchRadius) >= RequiredCount);

        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
            .FailOnDespawnedNullOrForbidden(TargetIndex.B);

        yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
            .FailOnDestroyedNullOrForbidden(TargetIndex.A);

        Toil dropAtAnchor = ToilMaker.MakeToil("AssembleCipher_DropAtAnchor");
        dropAtAnchor.initAction = () =>
        {
            Thing anchor = CipherFragment;
            Thing carried = pawn.carryTracker?.CarriedThing;
            if (anchor == null || anchor.Destroyed || carried == null)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (!pawn.carryTracker.TryDropCarriedThing(anchor.PositionHeld, ThingPlaceMode.Direct, out _))
                pawn.carryTracker.TryDropCarriedThing(anchor.PositionHeld, ThingPlaceMode.Near, out _);
        };
        yield return dropAtAnchor;

        yield return Toils_Jump.Jump(findNextRemoteSource);

        yield return goToAssemblePoint;
        yield return PrepareToUse();
        yield return Use();
    }
}
