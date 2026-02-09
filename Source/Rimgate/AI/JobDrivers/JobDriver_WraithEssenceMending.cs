using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using static UnityEngine.GridBrushBase;

namespace Rimgate;

public class JobDriver_WraithEssenceMending : JobDriver_CastVerbOnce
{
    private const float PostiveThoughtChance = 0.7f;

    public override string GetReport()
    {
        StringBuilder sb = new StringBuilder(ReportStringProcessed(job.def.reportString));
        if (job.ability != null && job.ability.def.showCastingProgressBar && job.verbToUse.WarmupTicksLeft > 0)
            sb.AppendLine($" {"DurationLeft".Translate(job.verbToUse.WarmupTicksLeft.ToStringTicksToPeriod(shortForm: true))}.");
        return sb.ToString().TrimEndNewlines();
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        Pawn actor = pawn;

        this.FailOn(() => job.ability == null || (!job.ability.CanCast && !job.ability.Casting));
        this.FailOn(() => actor.Dead || actor.Downed || !actor.Spawned || actor.InMentalState || actor.Drafted);

        yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.OnCell);

        Toil trance = ToilMaker.MakeToil("CastVerb");
        trance.initAction = delegate
        {
            actor.pather.StopDead();
            actor.jobs.curJob.verbToUse.TryStartCastOn(
                job.targetA,
                LocalTargetInfo.Invalid,
                surpriseAttack: false,
                false,
                actor.jobs.curJob.preventFriendlyFire);
        };
        trance.defaultDuration = job.verbToUse.verbProps.warmupTime.SecondsToTicks();
        trance.tickAction = delegate
        {
            pawn.GainComfortFromCellIfPossible(1);
            if (pawn.IsHashIntervalTick(120))
            {
                FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Meditating);

                if (!ModsConfig.RoyaltyActive || pawn.psychicEntropy == null)
                    return;

                pawn.psychicEntropy.Notify_Meditated();
                pawn.psychicEntropy.GainPsyfocus_NewTemp(1);
            }
        };
        trance.tickIntervalAction = delegate
        {
            actor.rotationTracker.FaceCell(actor.Position + IntVec3.South);
        };
        trance.AddFinishAction(ApplyWraithMeditationMemories);
        trance.defaultCompleteMode = ToilCompleteMode.Delay;
        if (job.ability != null && job.ability.def.showCastingProgressBar && job.verbToUse != null)
            trance.WithProgressBar(TargetIndex.None, () => job.verbToUse.WarmupProgress);
        trance.handlingFacing = true;
        trance.socialMode = RandomSocialMode.Off;
        yield return trance;

        AddFinishAction((jc) =>
        {
            if (jc != JobCondition.Succeeded)
                return;

            var ability = actor.abilities.GetAbility(RimgateDefOf.Rimgate_WraithEssenceMendingAbility);
            var costComp = ability?.CompOfType<CompAbilityEffect_EssenceCost>();
            if (costComp != null && !costComp.Props.payCostAtStart)
                BiologyUtil.OffsetEssenceCost(actor, 0f - costComp.Props.essenceCost);

            int totalToHeal = ability?.CompOfType<CompAbilityEffect_WraithEssenceMending>()?.Props?.totalChronicToHeal ?? 1;
            for (int i = 0; i < totalToHeal; i++)
            {
                if (MedicalUtil.TryHealOneChronic(actor) && actor.Spawned)
                    FleckMaker.ThrowMetaIcon(actor.Position, actor.Map, FleckDefOf.HealingCross);
            }

            if (job.ability != null && job.def.abilityCasting)
                job.ability.StartCooldown(job.ability.def.cooldownTicksRange.RandomInRange);
        });
    }

    public override void Notify_Starting()
    {
        base.Notify_Starting();
        job.ability?.Notify_StartedCasting();
    }

    private void ApplyWraithMeditationMemories()
    {
        // Only Wraith / hive-linked pawns get the special thoughts
        if (!pawn.HasHiveConnection())
            return;

        var def = Rand.Chance(PostiveThoughtChance)
            ? RimgateDefOf.Rimgate_WraithCommunedWithHive
            : RimgateDefOf.Rimgate_WraithWhispersFromVoid;

        pawn.TryGiveThought(def);
    }
}
