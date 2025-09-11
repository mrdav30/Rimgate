using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_PushMobileContainer : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        // Only buildings that actually have the comp
        foreach (var b in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
        {
            if (b.TryGetComp<Comp_MobileContainer>() != null)
                yield return b;
        }
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var comp = t.TryGetComp<Comp_MobileContainer>();
        if (comp == null) return false;

        if (comp.LoadingInProgress) return false;
        if (!pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly)) return false;
        if (!comp.FuelOK) return false;

        // If not forced, require either being the attached pusher or no pusher
        if (!forced && comp.Pusher != null && comp.Pusher != pawn)
            return false;

        return true;
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (Utils.PawnIncapableOfHauling(pawn, out _)) return true;

        // Skip if no idle carts to push (cheap scan)
        foreach (var b in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
        {
            var c = b.TryGetComp<Comp_MobileContainer>();
            if (c != null && !c.LoadingInProgress) return false;
        }
        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return JobMaker.MakeJob(RimgateDefOf.Rimgate_PushMobileContainer, t);
    }
}