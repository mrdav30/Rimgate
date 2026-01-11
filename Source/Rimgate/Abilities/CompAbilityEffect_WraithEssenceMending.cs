using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class CompAbilityEffect_WraithEssenceMending : CompAbilityEffect
{
    public new CompProperties_WraithEssenceMending Props => (CompProperties_WraithEssenceMending)props;

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        var targetCell = target.Cell;

        if(!Utils.IsGoodSpawnCell(targetCell, parent.pawn.Map))
        {
            if (throwMessages)
                Messages.Message("CannotReach".Translate(), MessageTypeDefOf.RejectInput);
            return false;
        }

        return true;
    }

    public override bool GizmoDisabled(out string reason)
    {
        reason = string.Empty;
        Pawn pawn = parent.pawn;
        if (pawn == null || pawn.Dead || pawn.Downed || pawn.InMentalState) return true;

        if (Props.healsChronic && !pawn.HasAnyChronic())
        {
            reason = "CannotUseAbility".Translate(parent.def.label) + ": No valid chronic conditions to heal.";
            return true;
        }

        return false;
    }
}
