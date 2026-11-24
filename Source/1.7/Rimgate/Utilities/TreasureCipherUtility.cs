using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate;

public static class TreasureCipherUtility
{
    public static List<Thing> CollectLocalFragments(Pawn pawn, Thing anchor, float nearbyRadius)
    {
        var result = new List<Thing>();
        if (pawn == null || anchor?.def == null)
            return result;

        var targetDef = anchor.def;
        var seen = new HashSet<int>();

        TryAddFragment(anchor, targetDef, seen, result);
        TryAddFragment(pawn.carryTracker?.CarriedThing, targetDef, seen, result);

        var inventory = pawn.inventory?.innerContainer;
        if (inventory != null)
        {
            for (int i = 0; i < inventory.Count; i++)
                TryAddFragment(inventory[i], targetDef, seen, result);
        }

        if (nearbyRadius <= 0f)
            return result;

        Map map = anchor.Spawned && anchor.MapHeld != null ? anchor.MapHeld : pawn.MapHeld;
        if (map == null)
            return result;

        IntVec3 center = anchor.Spawned && anchor.MapHeld != null ? anchor.PositionHeld : pawn.PositionHeld;
        foreach (var cell in GenRadial.RadialCellsAround(center, nearbyRadius, useCenter: true))
        {
            if (!cell.InBounds(map))
                continue;

            var things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                var thing = things[i];
                if (thing.IsForbidden(pawn))
                    continue;
                TryAddFragment(thing, targetDef, seen, result);
            }
        }

        return result;
    }

    public static int CountLocalFragments(Pawn pawn, Thing anchor, float nearbyRadius)
    {
        int total = 0;
        var fragments = CollectLocalFragments(pawn, anchor, nearbyRadius);
        for (int i = 0; i < fragments.Count; i++)
            total += fragments[i].stackCount;
        return total;
    }

    public static int CountColonyReachableFragments(Pawn pawn, Thing anchor)
    {
        if (pawn == null || anchor?.def == null)
            return 0;

        int total = 0;
        var targetDef = anchor.def;

        Thing carried = pawn.carryTracker?.CarriedThing;
        if (carried != null && carried.def == targetDef && !carried.Destroyed)
            total += carried.stackCount;

        var inventory = pawn.inventory?.innerContainer;
        if (inventory != null)
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                var thing = inventory[i];
                if (thing?.def == targetDef && !thing.Destroyed)
                    total += thing.stackCount;
            }
        }

        Map map = anchor.Spawned && anchor.MapHeld != null ? anchor.MapHeld : pawn.MapHeld;
        if (map == null)
            return total;

        var spawned = map.listerThings.ThingsOfDef(targetDef);
        for (int i = 0; i < spawned.Count; i++)
        {
            Thing thing = spawned[i];
            if (thing.Destroyed || thing.IsForbidden(pawn))
                continue;
            if (!pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Some))
                continue;
            total += thing.stackCount;
        }

        return total;
    }

    public static bool TryFindClosestRemoteFragment(Pawn pawn, Thing anchor, float nearbyRadius, out Thing fragment)
    {
        fragment = null;
        if (pawn == null || anchor?.def == null || !pawn.Spawned)
            return false;

        Map map = anchor.Spawned && anchor.MapHeld != null ? anchor.MapHeld : pawn.MapHeld;
        if (map == null)
            return false;

        fragment = GenClosest.ClosestThingReachable(
            pawn.Position,
            map,
            ThingRequest.ForDef(anchor.def),
            PathEndMode.ClosestTouch,
            TraverseParms.For(pawn),
            9999f,
            t => IsValidRemoteSource(pawn, anchor, nearbyRadius, t));

        return fragment != null;
    }

    private static bool IsValidRemoteSource(Pawn pawn, Thing anchor, float nearbyRadius, Thing thing)
    {
        if (thing == null || thing.Destroyed || !thing.Spawned || thing.def != anchor.def || thing.stackCount <= 0)
            return false;
        if (thing == anchor)
            return false;
        if (thing.IsForbidden(pawn))
            return false;
        if (!pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Some))
            return false;
        if (!pawn.CanReserve(thing))
            return false;
        int reservable = pawn.Map?.reservationManager?.CanReserveStack(pawn, thing) ?? 0;
        if (reservable <= 0)
            return false;
        if (IsWithinLocalAssembleArea(pawn, anchor, nearbyRadius, thing))
            return false;

        return true;
    }

    private static bool IsWithinLocalAssembleArea(Pawn pawn, Thing anchor, float nearbyRadius, Thing thing)
    {
        if (!thing.Spawned)
            return false;

        Map map = anchor.Spawned && anchor.MapHeld != null ? anchor.MapHeld : pawn.MapHeld;
        if (map == null || thing.MapHeld != map)
            return false;

        IntVec3 center = anchor.Spawned && anchor.MapHeld != null ? anchor.PositionHeld : pawn.PositionHeld;
        return thing.PositionHeld.InHorDistOf(center, nearbyRadius);
    }

    private static void TryAddFragment(Thing thing, ThingDef targetDef, HashSet<int> seen, List<Thing> results)
    {
        if (thing == null || thing.Destroyed || thing.def != targetDef || thing.stackCount <= 0)
            return;

        if (!seen.Add(thing.thingIDNumber))
            return;

        results.Add(thing);
    }
}
