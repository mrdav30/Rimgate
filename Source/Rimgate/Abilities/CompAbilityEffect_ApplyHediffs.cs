using DubsBadHygiene;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompAbilityEffect_ApplyHediffs : CompAbilityEffect
{
    public CompProperties_ApplyHediff Props => (CompProperties_ApplyHediff)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        if (target.Pawn == null)
            return;

        if (Props.hediffToReceive != null)
            Utils.ApplyHediff(
                parent.pawn,
                Props.hediffToReceive,
                null,
                GetDurationForPawn(parent.pawn),
                0);

        if (Props.hediffToGive == null)
            return;

        Utils.ApplyHediff(
            target.Pawn,
            Props.hediffToGive,
            null,
            GetDurationForPawn(target.Pawn),
            0);
    }

    private int GetDurationForPawn(Pawn pawn)
    {
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
        return statFactors.Aggregate(current, (float current, StatModifier statFactor) => {
            return statFactor.value >= 0f
                ? current * (pawn.GetStatValue(statFactor.stat) * statFactor.value)
                : current / (pawn.GetStatValue(statFactor.stat) * Math.Abs(statFactor.value));
        });
    }
}
