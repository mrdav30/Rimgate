using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_RecoverBiomaterialFromCorpse : JobDriver
{
    private Corpse _corpse => (Corpse)job.targetA.Thing;

    private BiomaterialRecoveryDef _cachedDef;

    public BiomaterialRecoveryDef RecoveryDef => _cachedDef ??= DefDatabase<BiomaterialRecoveryDef>.GetNamedSilentFail(job.dutyTag);

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOn(() => RecoveryDef == null);
        this.FailOn(() => !CorpseBiomaterialRecoveryUtility.IsCorpseValidForRecovery(_corpse, pawn));

        if (RecoveryDef.requiredKit != null)
        {
            // Find kit on map if not already carrying enough
            Toil findKit = ToilMaker.MakeToil("RecoverBiomaterial_FindKit");
            findKit.initAction = () =>
            {
                // already carrying enough?
                Thing carried = pawn.carryTracker?.CarriedThing;
                if (carried?.def == RecoveryDef.requiredKit && carried.stackCount >= 1)
                    return;

                if (!CorpseBiomaterialRecoveryUtility.TryGetClosestKit(pawn, RecoveryDef.requiredKit, out Thing kit))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int num = pawn.Map.reservationManager.CanReserveStack(pawn, kit);
                if (num <= 0 || !pawn.Reserve(kit, job, stackCount: 1))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                job.SetTarget(TargetIndex.B, kit);
                job.count = 1;
            };
            yield return findKit;

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B);

            // Take into carryTracker
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
        }

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

        // “Work” toil
        Toil work = ToilMaker.MakeToil("RecoverBiomaterial_Work");
        work.initAction = delegate
        {
            // one more gating pass so it can fail gracefully if something changed
            if (!CorpseBiomaterialRecoveryUtility.CanStartRecoveryJob(pawn, _corpse, RecoveryDef, out _, false))
                EndJobWith(JobCondition.Incompletable);
        };

        work.tickAction = delegate
        {
            pawn.skills?.Learn(RecoveryDef.workSkill, RecoveryDef.workSkillLearnFactor);
        };

        work.defaultCompleteMode = ToilCompleteMode.Delay;
        work.defaultDuration = RecoveryDef.GetWorkTicks(pawn);
        work.WithEffect(RecoveryDef.effectWorking, TargetIndex.A);
        work.PlaySustainerOrSound(() => RecoveryDef.soundWorking);
        work.FailOn(() =>
        {
            if (RecoveryDef.requiredKit == null) return false;
            var c = pawn.carryTracker?.CarriedThing;
            return c?.def != RecoveryDef.requiredKit || c.stackCount < 1;
        });
        work.WithProgressBarToilDelay(TargetIndex.A);

        yield return work;

        // Finish: do the recovery + consume kit
        Toil finish = ToilMaker.MakeToil("RecoverBiomaterial_Finish");
        finish.initAction = delegate
        {
            var corpse = _corpse;
            bool ok = CorpseBiomaterialRecoveryUtility.TryRecoverFromCorpse(
                pawn,
                corpse,
                RecoveryDef,
                consumeKitOnAttempt: true,
                out var spawnedThing,
                out string reason);

            if (!ok)
            {
                Messages.Message("RG_BiomaterialRecoveryMessage_Failed".Translate(corpse.InnerPawn.LabelShort, reason),
                    new TargetInfo(corpse.Position, corpse.Map),
                    MessageTypeDefOf.NegativeEvent);
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            Messages.Message("RG_BiomaterialRecoveryMessage_Procured".Translate(spawnedThing.LabelShort, corpse.InnerPawn.LabelShort),
                new TargetInfo(corpse.Position, corpse.Map),
                MessageTypeDefOf.PositiveEvent);
        };

        yield return finish;
    }
}
