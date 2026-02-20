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
            if (t == null || t is not Building_MobileContainer) continue;
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
        if (t is not Building_MobileContainer container || container.Control == null) return false;

        if (pawn.Map.designationManager.DesignationOn(t, RimgateDefOf.Rimgate_DesignationPushCart) == null)
            return false;

        if (container.Control.LoadingInProgress) return false;
        if (!pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Deadly)) return false;
        if (!container.Control.FuelOK) return false;

        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Comp_MobileContainerControl control = (t as Building_MobileContainer).Control;
        if (control == null) return null;
        var job = control.GetDesignatedPushJob(pawn);
        if (job == null) return null;
        job.playerForced = forced;
        return job;
    }
}
