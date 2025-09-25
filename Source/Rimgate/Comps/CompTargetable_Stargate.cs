using System;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class CompTargetable_Stargate : CompTargetable
{
    protected override bool PlayerChoosesTarget => true;

    protected override TargetingParameters GetTargetingParameters()
    {
        return new TargetingParameters
        {
            validator = (TargetInfo x) =>
            {
                Comp_StargateControl sgComp = x.Thing.TryGetComp<Comp_StargateControl>();
                bool canTarget = x.Thing != null 
                    && sgComp != null 
                    && sgComp.Props.canHaveIris 
                    && !sgComp.HasIris;
 
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
