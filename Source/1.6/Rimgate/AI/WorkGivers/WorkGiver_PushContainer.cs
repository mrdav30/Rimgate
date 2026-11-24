using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_PushContainer : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        var dm = pawn?.Map?.designationManager;
        if (dm == null) yield break;

        foreach (var item in dm.SpawnedDesignationsOfDef(RimgateDefOf.Rimgate_DesignationPushCart))
        {
            Thing t = item?.target.Thing;
            if (t is not Building_MobileContainer container || !TargetIsValid(container)) continue;
            yield return t;
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (pawn.IncapableOfHauling(out _)) return true;

        var dm = pawn?.Map?.designationManager;
        if (dm == null) return true;

        return !dm.AnySpawnedDesignationOfDef(RimgateDefOf.Rimgate_DesignationPushCart);
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_MobileContainer container || !TargetIsValid(container)) return false;

        if (pawn.Map.designationManager.DesignationOn(container, RimgateDefOf.Rimgate_DesignationPushCart) == null)
            return false;

        if (!pawn.CanReserveAndReach(container, PathEndMode.InteractionCell, Danger.Deadly)) return false;

        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_MobileContainer container || !TargetIsValid(container)) return null;
        var job = container.GetPushJob(container.DesignatedTarget, container.WantsToBeDumped, false);
        if (job == null) return null;
        job.playerForced = forced;
        job.count = 1;
        return job;
    }

    private bool TargetIsValid(Building_MobileContainer container)
    {
        if (container == null || container.LoadingInProgress || !container.FuelOK || !container.HasPushTarget) return false;
        return true;
    }
}
