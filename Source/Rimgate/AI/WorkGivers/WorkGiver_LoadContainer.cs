using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimgate;

public class WorkGiver_LoadContainer : WorkGiver_Scanner
{
    // Scan buildings; we’ll filter down to our carts in PotentialWorkThingsGlobal
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    // Fast skip: if no cart on the map is actively loading, don’t even run the expensive scan
    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (pawn.IncapableOfHauling(out _)) return true;

        var list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
        for (int i = 0; i < list.Count; i++)
        {
            if(list[i] is not Building_MobileContainer cart) continue;
            if (cart.LoadingInProgress && cart.AnyPawnCanLoadAnythingNow)
                return false;
        }

        return true;
    }

    // Yield only the carts that actually need hauling
    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        var list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
        for (int i = 0; i < list.Count; i++)
        {
            if(list[i] is not Building_MobileContainer cart) continue;
            if (!cart.LoadingInProgress) continue;
            if (!pawn.CanReserveAndReach(cart, PathEndMode.Touch, Danger.Deadly)) continue;

            // Optional: make sure this pawn actually has something he can haul to this cart
            if (!MobileContainerUtility.HasJobOnContainer(pawn, cart)) continue;

            yield return cart;
        }
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_MobileContainer container) return false;
        return container.LoadingInProgress
            && pawn.CanReserveAndReach(container, PathEndMode.Touch, Danger.Deadly)
            && MobileContainerUtility.HasJobOnContainer(pawn, container);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_MobileContainer container) return null;
        var job = JobMaker.MakeJob(RimgateDefOf.Rimgate_HaulToContainer, LocalTargetInfo.Invalid, container);
        job.ignoreForbidden = true;
        // (Optional) slightly prioritize this job when player-initiated loading is active
        return job;
    }
}