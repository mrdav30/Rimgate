using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobDriver_DrainLifeFromCocoonedPrisoner : JobDriver
{
    public const int WaitTicks = 120;

    private Building_WraithCocoonPod Pod => (Building_WraithCocoonPod)job.targetA.Thing;

    private Pawn Victim => Pod.ContainedThing as Pawn;

    private CompProperties_ApplyDrainLife _cachedProps;

    private CompProperties_ApplyDrainLife Props => _cachedProps ??= job.ability?.CompOfType<CompAbilityEffect_ApplyDrainLife>()?.Props;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => !Pod.HasAnyContents || Props == null);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

        Toil wait = Toils_General.Wait(WaitTicks, TargetIndex.A);
        wait.FailOnCannotTouch(TargetIndex.A, PathEndMode.ClosestTouch);
        wait.WithProgressBarToilDelay(TargetIndex.A);
        yield return wait;

        yield return Toils_General.Do(delegate
        {
            var victim = Victim;
            if (Props.hediffToReceive != null)
                pawn.ApplyHediff(
                    Props.hediffToReceive,
                    severity: Props.hediffToReceiveSeverity);

            if (Props.hediffToGive != null)
                victim.ApplyHediff(
                    Props.hediffToGive,
                    severity: Props.hediffToGiveSeverity);

            BiologyUtil.AdjustResourceGain(pawn, victim, Props.geneDef, Props.essenceGainAmount, Props.allowFreeDraw, Props.affects);

            if (Props.thoughtToGiveTarget != null)
                victim.TryGiveThought(Props.thoughtToGiveTarget, pawn);

            if (Props.opinionThoughtToGiveTarget != null)
                victim.TryGiveThought(Props.opinionThoughtToGiveTarget, pawn);

            var cooldown = job.ability.def.cooldownTicksRange.RandomInRange;
            job.ability.StartCooldown(cooldown);
        });
    }
}
