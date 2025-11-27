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

public class CompAbilityEffect_ApplyHediff : CompAbilityEffect
{
    public CompProperties_ApplyHediff Props => (CompProperties_ApplyHediff)props;

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        Pawn pawn = parent?.pawn;
        Pawn pawn2 = target.Pawn;
        if (pawn == null 
            || pawn2 == null 
            || pawn2.Dead
            || pawn2.RaceProps.IsMechanoid
            ||  pawn.ThingID == pawn2.ThingID)
            return false;

        if(Props.rangeLimit > 0)
        {
            var distance = pawn.Position.DistanceToSquared(pawn2.Position);
            return distance <= (Props.rangeLimit * Props.rangeLimit);
        }

        return true;
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        if (target.Pawn == null)
            return;

        if (Props.hediffToReceive != null)
            parent.pawn.ApplyHediff(
                Props.hediffToReceive,
                severity: 0,
                duration: GetDurationForPawn(parent.pawn));

        if (Props.hediffToGive == null)
            return;

        target.Pawn.ApplyHediff(
            Props.hediffToGive,
            severity: 0,
            duration: GetDurationForPawn(parent.pawn));
    }

    private int GetDurationForPawn(Pawn pawn)
    {
        if (Props.durationTimeStatFactors == null) 
            return Props.durationTime;

        return Mathf.RoundToInt(
            CalculateModifiedStatForPawn(
                pawn,
                Props.durationTime,
                Props.durationTimeStatFactors));
    }

    public virtual float CalculateModifiedStatForPawn(
        Pawn pawn,
        float current,
        IEnumerable<StatModifier> statFactors)
    {
        return statFactors.Aggregate(current, (float current, StatModifier statFactor) =>
        {
            return statFactor.value >= 0f
                ? current * (pawn.GetStatValue(statFactor.stat) * statFactor.value)
                : current / (pawn.GetStatValue(statFactor.stat) * Math.Abs(statFactor.value));
        });
    }
}
