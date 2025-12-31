using DubsBadHygiene;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace Rimgate;

public class CompAbilityEffect_ApplyDrainLife : CompAbilityEffect
{
    public CompProperties_ApplyDrainLife Props => (CompProperties_ApplyDrainLife)props;

    // AI can only target pawns within range limits
    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        Pawn pawn = parent?.pawn;
        Pawn pawn2 = target.Pawn;
        if (pawn == null
            || pawn2 == null
            || pawn2.Dead
            || !pawn2.RaceProps.IsFlesh
            || pawn.ThingID == pawn2.ThingID)
            return false;

        if (!Props.aiRangeLimit.IsValid) return true;

        var distance = pawn.Position.DistanceToSquared(pawn2.Position);
        return distance >= Props.MinRangeSquared
            && distance <= Props.MaxRangeSquared;
    }

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        if (!base.Valid(target, throwMessages))
            return false;

        if (target.Thing is Building_WraithCocoonPod pod && !pod.HasAnyContents)
            return false;

        return parent.pawn?.GetActiveGeneOf(Props.geneDef) != null;
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        Pawn caster = parent.pawn;
        Pawn victim = null;
        if (target.Thing is Building_WraithCocoonPod pod)
            victim = pod.ContainedThing as Pawn;
        else
            victim = target.Pawn;

        if (caster == null || victim == null)
            return;

        if (Props.hediffToReceive != null)
            caster.ApplyHediff(Props.hediffToReceive, severity: Props.hediffToReceiveSeverity);

        if (Props.hediffToGive != null)
            victim.ApplyHediff(Props.hediffToGive, severity: Props.hediffToGiveSeverity);

        BiologyUtil.AdjustResourceGain(caster, victim, Props.geneDef, Props.essenceGainAmount, Props.allowFreeDraw, Props.affects);

        if (Props.thoughtToGiveTarget != null)
            victim.TryGiveThought(Props.thoughtToGiveTarget, caster);

        if (Props.opinionThoughtToGiveTarget != null)
            victim.TryGiveThought(Props.opinionThoughtToGiveTarget, caster);

        base.Apply(target, dest);
    }
}
