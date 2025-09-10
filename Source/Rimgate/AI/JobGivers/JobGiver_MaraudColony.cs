using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public class JobGiver_MaraudColony : ThinkNode_JobGiver
{
    private static readonly IntRange _trashJobCheckOverrideInterval = new IntRange(450, 500);

    protected override Job TryGiveJob(Pawn pawn)
    {
        if (!GenHostility.HostileTo(pawn, Faction.OfPlayer))
            return null;

        bool hasExitSpot = RCellFinder.TryFindBestExitSpot(
            pawn,
            out IntVec3 intVec3_1,
            TraverseMode.ByPawn,
            true);
        if (hasExitSpot)
        {
            bool hasSteableItem = TryFindBestItemToSteal(
                pawn.Position,
                pawn.Map,
                50f,
                out Thing thing1,
                pawn,
                danger: Danger.Some);
            if (hasSteableItem)
            {
                Job job = JobMaker.MakeJob(JobDefOf.Steal);
                job.targetA = thing1;
                job.targetB = intVec3_1;
                job.canBashDoors = true;
                job.canBashFences = true;

                int maxCount = (int)(StatExtension.GetStatValue(pawn, StatDefOf.CarryingCapacity)
                    / thing1.def.VolumePerUnit);
                job.count = Math.Min(thing1.stackCount, maxCount);
                return job;
            }
        }

        bool flag = pawn.natives.IgniteVerb != null
            && (pawn.natives.IgniteVerb).IsStillUsableBy(pawn)
            && GenHostility.HostileTo(pawn, Faction.OfPlayer);
        CellRect cellRect = CellRect.CenteredOn(pawn.Position, 5);
        for (int index = 0; index < 35; ++index)
        {
            IntVec3 randomCell = cellRect.RandomCell;
            if (GenGrid.InBounds(randomCell, pawn.Map))
            {
                Building edifice = GridsUtility.GetEdifice(randomCell, pawn.Map);
                if (edifice != null && TrashUtility.ShouldTrashBuilding(pawn, edifice, false))
                {

                    bool hasLos = GenSight.LineOfSight(pawn.Position, randomCell, pawn.Map);
                    if (hasLos)
                    {
                        Job job = TrashJob(pawn, edifice);
                        if (job != null)
                            return job;
                    }
                }

                if (flag)
                {
                    Plant plant = GridsUtility.GetPlant(randomCell, pawn.Map);
                    if (plant != null && TrashUtility.ShouldTrashPlant(pawn, plant))
                    {
                        bool hasLos = GenSight.LineOfSight(pawn.Position, randomCell, pawn.Map);
                        if (hasLos)
                        {
                            Job job = TrashJob(pawn, plant);
                            if (job != null)
                                return job;
                        }
                    }
                }
            }
        }

        List<Building> buildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
        if (buildingsColonist.Count == 0)
            return null;

        var randomBuildings = buildingsColonist
            .OrderBy<Building, float>(x =>
                IntVec3Utility.DistanceTo(x.Position, pawn.Position))
            .Take<Building>(10)
            .InRandomOrder<Building>();
        foreach (Building t in randomBuildings)
        {
            if (TrashUtility.ShouldTrashBuilding(pawn, t, true))
            {
                bool isReachable = ReachabilityUtility.CanReach(pawn, t, PathEndMode.Touch, Danger.None);
                if (isReachable)
                {
                    Job job = TrashJob(pawn, (Thing)t, true);
                    if (job != null)
                        return job;
                }
            }
        }

        if (!RCellFinder.TryFindBestExitSpot(pawn, out IntVec3 intVec3_2, TraverseMode.ByPawn, true)
            || !TryFindBestItemToSteal(pawn.Position, pawn.Map, 100f, out Thing thing2, pawn, danger: Danger.None))
            return null;

        Job job1 = JobMaker.MakeJob(JobDefOf.Steal);
        job1.targetA = thing2;
        job1.targetB = intVec3_2;
        job1.canBashDoors = true;
        job1.canBashFences = true;

        int maxCount2 = (int)(StatExtension.GetStatValue(pawn, StatDefOf.CarryingCapacity) / thing2.def.VolumePerUnit);
        job1.count = (int)Mathf.Min(thing2.stackCount, maxCount2);
        return job1;
    }

    public static bool TryFindBestItemToSteal(
    IntVec3 root,
    Map map,
    float maxDist,
    out Thing item,
    Pawn thief,
    List<Thing> disallowed = null,
    Danger danger = Danger.Some)
    {
        item = null;
        if (map == null) return false;

        if (thief != null && !thief.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            return false;

        // Ensure there’s a path out to the map edge so stealing is viable.
        bool viable;
        if (thief != null)
        {
            viable = map.reachability.CanReachMapEdge(
                thief.Position,
                TraverseParms.For(thief, Danger.Some, TraverseMode.ByPawn));
        }
        else
        {
            viable = map.reachability.CanReachMapEdge(
                root,
                TraverseParms.For(TraverseMode.PassDoors, Danger.Some));
        }
        if (!viable) return false;

        // 1) If the pawn has a duty focus (e.g., the ZPM), try to steal that first.
        if (thief?.mindState?.duty != null && thief.mindState.duty.focus.IsValid)
        {
            Thing focusThing = thief.mindState.duty.focus.Thing;
            if (focusThing != null
                && !focusThing.Destroyed
                && focusThing.Map == map
                && !FireUtility.IsBurning(focusThing)
                && (thief == null || ReservationUtility.CanReserve(thief, focusThing))
                && ReachabilityUtility.CanReach(thief, focusThing, PathEndMode.Touch, danger))
            {
                // Allow “objective” targets like the ZPM even if not flagged stealable.
                bool isObjective =
                    focusThing.def.defName == "Rimgate_ZPM" ||
                    focusThing == thief.mindState.duty.focus.Thing; // safety net for other objective defs

                // If it’s not the pawn’s own faction and either stealable or an objective, grab it.
                if (focusThing.Faction != thief.Faction && (focusThing.def.stealable || isObjective))
                {
                    item = focusThing;
                    return true;
                }
            }
            // If the focus exists but is unreachable/invalid, we intentionally fall through to standard looting.
        }

        // 2) Standard value-driven looting as fallback.
        Predicate<Thing> predicate = t =>
        {
            if (t.def.defName.IndexOf("chunk", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (t.def.IsCorpse || t.def.IsWeapon) return false;
            if (FireUtility.IsBurning(t)) return false;
            if (t.Faction == thief.Faction) return false;
            if (disallowed != null && disallowed.Contains(t)) return false;
            if (thief != null && !ReservationUtility.CanReserve(thief, t)) return false;
            if (!t.def.stealable) return false;
            return GetValue(t) >= 10.0f;
        };

        item = GenClosest.ClosestThing_Regionwise_ReachablePrioritized(
            root,
            map,
            ThingRequest.ForGroup(ThingRequestGroup.HaulableEverOrMinifiable),
            PathEndMode.ClosestTouch,
            TraverseParms.For(TraverseMode.PassDoors, danger),
            maxDist,
            predicate,
            t => GetValue(t),
            15,
            15);

        return item != null;
    }


    public static float GetValue(Thing thing) => thing.MarketValue * (float)thing.stackCount;

    public static Job TrashJob(
        Pawn pawn,
        Thing t,
        bool allowPunchingInert = false,
        bool killIncappedTarget = false)
    {
        if (t is Plant)
        {
            Job job = JobMaker.MakeJob(JobDefOf.Ignite, t);
            FinalizeTrashJob(job);
            return job;
        }

        Job job1;
        bool canBurn = pawn.natives.IgniteVerb != null
            && pawn.natives.IgniteVerb.IsStillUsableBy(pawn)
            && t.FlammableNow
            && !FireUtility.IsBurning(t)
            && !(t is Building_Door);
        if (canBurn)
            job1 = JobMaker.MakeJob(JobDefOf.Ignite, t);
        else
            job1 = JobMaker.MakeJob(JobDefOf.AttackMelee, t);

        job1.killIncappedTarget = killIncappedTarget;
        FinalizeTrashJob(job1);
        return job1;
    }

    private static void FinalizeTrashJob(Job job)
    {
        Job job1 = job;
        IntRange overrideInterval = _trashJobCheckOverrideInterval;
        int randomInRange = overrideInterval.RandomInRange;
        job1.expiryInterval = randomInRange;
        job.checkOverrideOnExpire = true;
        job.expireRequiresEnemiesNearby = true;
    }
}