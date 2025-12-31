using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class CompAbilityEffect_WraithCocoonWrap : CompAbilityEffect
{
    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        Pawn targetPawn = target.Pawn;
        Pawn caster = parent.pawn;

        if (targetPawn == null)
        {
            if (throwMessages)
                SendPostProcessedMessage("RG_Abilitiy_TargetMustBePawn".Translate(), null, parent);
            return false;
        }

        if (targetPawn.Dead || !targetPawn.Spawned)
        {
            if (throwMessages)
                SendPostProcessedMessage("RG_Abilitiy_TargetMustBeAlive".Translate(targetPawn.Named("PAWN")), targetPawn, parent);
            return false;
        }

        if (!targetPawn.IsPrisonerOfColony)
        {
            if (throwMessages)
                SendPostProcessedMessage("RG_Abilitiy_TargetMustBeColonyPrisoner".Translate(targetPawn.Named("PAWN")), targetPawn, parent);
            return false;
        }

        if (targetPawn.Map != caster.Map)
        {
            if (throwMessages)
                SendPostProcessedMessage("RG_Abilitiy_TargetMustBeSameMap".Translate(), null, parent);
            return false;
        }

        if (targetPawn.ParentHolder is Building_WraithCocoonPod)
        {
            if (throwMessages)
                SendPostProcessedMessage("RG_WraithCocoonTrap_AlreadyCocooned".Translate(targetPawn.Named("PAWN")), targetPawn, parent);
            return false;
        }

        return true;
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => target.Pawn?.IsPrisonerOfColony == true;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        Pawn caster = parent.pawn;
        Pawn victim = target.Pawn;

        if (victim == null || !victim.Spawned || victim.Dead)
            return;

        // Start the cocooning job
        Job job = JobMaker.MakeJob(RimgateDefOf.Rimgate_WraithCocoonPrisoner, victim);
        job.playerForced = true; // show as player ordered
        job.count = 1;

        if (caster.jobs.TryTakeOrderedJob(job, JobTag.MiscWork))
            caster.rotationTracker.FaceTarget(victim);
    }

    private static void SendPostProcessedMessage(string message, LookTargets targets, Ability ability)
    {
        if (ability != null)
            message = "CannotUseAbility".Translate(ability.def.label) + ": " + message;

        if (targets != null && targets.Any)
            Messages.Message(message, targets, MessageTypeDefOf.RejectInput, historical: false);
        else
            Messages.Message(message, MessageTypeDefOf.RejectInput, historical: false);
    }
}
