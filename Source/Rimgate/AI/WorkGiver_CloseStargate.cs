using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_CloseStargate : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn)
    {
        return Danger.Deadly;
    }

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (Designation item in pawn.Map.designationManager.designationsByDef[Rimgate_DefOf.Rimgate_DesignationCloseStargate])
        {
            yield return item.target.Thing;
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(Rimgate_DefOf.Rimgate_DesignationCloseStargate);
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (pawn.Map.designationManager.DesignationOn(t, Rimgate_DefOf.Rimgate_DesignationCloseStargate) == null)
        {
            return false;
        }

        if (!pawn.CanReserve(t, 1, -1, null, forced))
        {
            return false;
        }

        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return JobMaker.MakeJob(Rimgate_DefOf.Rimgate_CloseStargate, t);
    }
}
