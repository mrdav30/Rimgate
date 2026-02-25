using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public static class MobileContainerUtility
{
    private static HashSet<Thing> neededThings = new();

    private static Dictionary<TransferableOneWay, int> tmpAlreadyLoading = new();

    public static bool HasJobOnContainer(Pawn pawn, Building_MobileContainer container)
    {
        if (!container.LoadingInProgress)
            return false;

        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            return false;

        if (!pawn.CanReach(container, PathEndMode.Touch, pawn.NormalMaxDanger()))
            return false;

        if (FindThingToLoad(pawn, container).Thing == null)
            return false;

        return true;
    }

    public static ThingCount FindThingToLoad(Pawn p, Building_MobileContainer container)
    {
        neededThings.Clear();
        List<TransferableOneWay> leftToLoad = container.LeftToLoad;
        tmpAlreadyLoading.Clear();
        if (leftToLoad != null)
        {
            IReadOnlyList<Pawn> allPawnsSpawned = container.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                if (allPawnsSpawned[i] == p || allPawnsSpawned[i].CurJobDef != RimgateDefOf.Rimgate_HaulToContainer)
                    continue;

                if (allPawnsSpawned[i].jobs.curDriver is not JobDriver_HaulToMobileContainer jobDriver
                    || jobDriver.MobileContainer.ThingID != container.ThingID)
                    continue;

                TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(jobDriver.ThingToCarry, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
                if (transferableOneWay != null)
                {
                    int value = 0;
                    if (tmpAlreadyLoading.TryGetValue(transferableOneWay, out value))
                        tmpAlreadyLoading[transferableOneWay] = value + jobDriver.InitialCount;
                    else
                        tmpAlreadyLoading.Add(transferableOneWay, jobDriver.InitialCount);
                }
            }

            for (int j = 0; j < leftToLoad.Count; j++)
            {
                TransferableOneWay transferableOneWay2 = leftToLoad[j];
                if (!tmpAlreadyLoading.TryGetValue(leftToLoad[j], out var value2))
                    value2 = 0;

                if (transferableOneWay2.CountToTransfer - value2 > 0)
                {
                    for (int k = 0; k < transferableOneWay2.things.Count; k++)
                        neededThings.Add(transferableOneWay2.things[k]);
                }
            }
        }

        if (!neededThings.Any())
        {
            tmpAlreadyLoading.Clear();
            return default;
        }

        Thing thing = GenClosest.ClosestThingReachable(
            p.Position,
            p.Map,
            ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
            PathEndMode.Touch,
            TraverseParms.For(p),
            9999f,
            x => neededThings.Contains(x)
                && x.Spawned
                && x.PositionHeld.DistanceToSquared(container.Position) <= container.Ext.SqrdLoadRadius  // radius gate
                && p.CanReserve(x)
                && !x.IsForbidden(p)
                && p.carryTracker.AvailableStackSpace(x.def) > 0);

        neededThings.Clear();
        if (thing != null)
        {
            TransferableOneWay transferableOneWay3 = null;
            for (int l = 0; l < leftToLoad.Count; l++)
            {
                if (leftToLoad[l].things.Contains(thing))
                {
                    transferableOneWay3 = leftToLoad[l];
                    break;
                }
            }

            if (!tmpAlreadyLoading.TryGetValue(transferableOneWay3, out var value3))
                value3 = 0;

            tmpAlreadyLoading.Clear();
            return new ThingCount(thing, Mathf.Min(transferableOneWay3.CountToTransfer - value3, thing.stackCount));
        }

        tmpAlreadyLoading.Clear();
        return default(ThingCount);
    }

    public static IEnumerable<Thing> ThingsBeingHauledTo(Building_MobileContainer container, Map map)
    {
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            bool isValid = pawn.CurJobDef == RimgateDefOf.Rimgate_HaulToContainer
                && pawn.jobs.curDriver is JobDriver_HaulToMobileContainer jd
                && jd.MobileContainer.ThingID == container.ThingID
                && pawn.carryTracker.CarriedThing != null;
            if (isValid)
                yield return pawn.carryTracker.CarriedThing;
        }
    }

    public static IEnumerable<Thing> AllSendableItems(Building_MobileContainer container, Map map)
    {
        var items = CaravanFormingUtility.AllReachableColonyItems(
            map,
            true,
            container.Ext.canChangeAssignedThingsAfterStarting && container.LoadingInProgress,
            true);

        float r2 = container.Ext.SqrdLoadRadius;
        IntVec3 center = container.Position;

        for (int i = 0; i < items.Count; i++)
        {
            var t = items[i];
            // Spawned + within radius of the cart (use PositionHeld so items in containers count correctly)
            if (t.Spawned
                && !t.IsForbidden(Faction.OfPlayer)
                && t.PositionHeld.DistanceToSquared(center) <= r2)
                yield return t;
        }
    }
}
