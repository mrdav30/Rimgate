using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class CompTargetable_TokraTunnelCrystal : CompTargetable
{
    protected override bool PlayerChoosesTarget => true;

    // only allow targeting mineable rocks with a Mine designation, to avoid confusion and ensure the effect works
    protected override TargetingParameters GetTargetingParameters()
    {
        return new TargetingParameters
        {
            canTargetLocations = false,
            canTargetPawns = false,
            canTargetBuildings = true, // mineable rocks are Buildings
            canTargetItems = false,
            validator = targ =>
            {
                if (!targ.IsValid || !targ.Cell.IsValid) return false;
                Map map = parent.Map;
                if (map == null) return false;
                IntVec3 c = targ.Cell;
                if (!c.InBounds(map)) return false;

                if(targ.Thing is not Mineable mineable) return false;

                Designation des = map.designationManager.DesignationAt(c, DesignationDefOf.Mine);
                if (des == null) return false;

                return true;
            }
        };
    }

    public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
    {
        yield return targetChosenByPlayer;
        yield break;
    }
}
