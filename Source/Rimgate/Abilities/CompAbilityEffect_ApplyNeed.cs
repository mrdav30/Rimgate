using DubsBadHygiene;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimgate;

public class CompAbilityEffect_ApplyNeed : CompAbilityEffect
{
    public CompProperties_ApplyNeed Props => (CompProperties_ApplyNeed)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        Pawn current = parent.pawn;
        if (current == null)
            return;

        if (current.needs != null && current.needs.TryGetNeed(Props.need, out var need))
        {
            float amount = Props.levelRange.RandomInRange;
            need.CurLevelPercentage = Mathf.Clamp01(need.CurLevelPercentage + amount);
        }
    }
}
