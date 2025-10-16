// CompProperties_AdjustNeedOnCast.cs
using RimWorld;
using System;
using Verse;

namespace Rimgate;

public class CompAbilityEffect_AdjustNeedOnCast : CompAbilityEffect
{
    public new CompProperties_AdjustNeedOnCast Props 
        => (CompProperties_AdjustNeedOnCast)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        if (Props.needDef == null || Props.amount == 0f) return;

        Pawn casterPawn = parent.pawn;
        Pawn targetPawn = target.Pawn;

        if (Props.onlyIfTargetHarmed 
            && targetPawn != null 
            && !targetPawn.Dead)
        {
            // optional: verify target actually took damage/hediff this tick (lightweight)
            if (!targetPawn.health.hediffSet.hediffs.Any(h => h.ageTicks < 30 && h.Visible))
                return;
        }

        Pawn who = Props.affects == "Target"
            ? targetPawn 
            : casterPawn;
        if (who == null || who.needs == null) return;

        Need need = who.needs.TryGetNeed(Props.needDef);
        if (need == null) return;

        need.CurLevel = Math.Min(need.MaxLevel, need.CurLevel + Props.amount / 100f);
    }

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        if (!base.Valid(target, throwMessages)) return false;
        return true;
    }
}
