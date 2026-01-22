using System;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class CompTargetable_Gate : CompTargetable
{
    protected override bool PlayerChoosesTarget => true;

    protected override TargetingParameters GetTargetingParameters()
    {
        return new TargetingParameters
        {
            validator = (TargetInfo x) =>
            {
                Building_Gate gate = x.Thing as Building_Gate;
                bool canTarget = gate != null 
                    && gate.Props.canHaveIris 
                    && !gate.HasIris;
 
                return canTarget;
            }
        };
    }

    public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
    {
        yield return targetChosenByPlayer;
        yield break;
    }
}
