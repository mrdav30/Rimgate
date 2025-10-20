using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Rimgate;

public static class StargateUtility
{
    public const string Alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private const int MaxGateSearchRadius = 30;   // near-ish center

    private const int MaxTries = 2000;

    public static bool ActiveGateOnMap(Map map)
    {
        Building_Stargate gate = Building_Stargate.GetStargateOnMap(map);
        if (gate == null) return false;
        if (gate.StargateControl == null) return false;
        return gate.StargateControl.IsActive;
    }

    public static string GetStargateDesignation(PlanetTile address)
    {
        if (!address.Valid)
            return "UnknownLower".Translate();

        Rand.PushState(address);
        // pattern: (layerDesignation)(num)(char)-(num)(num)(num)
        // Planet layer designation: O for orbit / space, P for planetary / other
        string layerDesignation = address.Layer.Def.isSpace
            ? "O"
            : "P";
        string designation =
            $"{layerDesignation}{Rand.RangeInclusive(0, 9)}{Alpha[Rand.RangeInclusive(0, 25)]}"
            + $"-{Rand.RangeInclusive(0, 9)}{Rand.RangeInclusive(0, 9)}{Rand.RangeInclusive(0, 9)}";
        Rand.PopState();
        return designation;
    }

    public static Building_Stargate PlaceRandomGateAndDHD(Map map)
    {
        // 1) Find a safe spot for a new gate
        // (unfogged, standable, no edifice, not roofed)
        IntVec3 spot;
        if (!TryFindGateSpot(map, out spot))
            spot = CellFinderLoose.RandomCellWith(c => c.Standable(map), map);

        var gate = GenSpawn.Spawn(
            RimgateDefOf.Rimgate_Stargate,
            spot,
            map,
            Rot4.North,
            WipeMode.Vanish) as Building_Stargate;
        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Spawned a fallback Stargate at {spot} on {map}.");

        // 2) Make sure a DHD exists within facility link radius (defaults to 8)
        EnsureDhdNearGate(map, gate);

        return gate;
    }

    private static bool TryFindGateSpot(Map map, out IntVec3 spot)
    {
        var center = map.Center;
        // Look near center first; avoid fog/roof/edifice
        return CellFinder.TryFindRandomCellNear(
            center,
            map,
            MaxGateSearchRadius,
            c => c.Standable(map)
                && !c.Fogged(map)
                && !c.Roofed(map)
                && c.GetEdifice(map) == null
                && map.reachability.CanReach(c, center, PathEndMode.OnCell, TraverseMode.PassDoors, Danger.Deadly),
            out spot);
    }

    private static void EnsureDhdNearGate(Map map, Building_Stargate gate)
    {
        // Any DHD within ~8 tiles (Facility default) is fine
        bool HasLinkedDhdNearby()
        {
            return map.listerThings.AllThings.Any(t =>
                t is Building_DHD
                && t.Spawned
                && t.Position.InHorDistOf(gate.Position, 8f));
        }

        if (HasLinkedDhdNearby())
            return;

        // Find a near spot (3..6 tiles) that’s clean for a 1x1 DHD
        IntVec3 near;
        bool found = CellFinder.TryFindRandomReachableNearbyCell(
            gate.Position,
            map,
            6f,
            TraverseParms.For(TraverseMode.PassDoors),
            c => c.Standable(map)
                && !c.Roofed(map)
                && !c.Fogged(map)
                && c.GetEdifice(map) == null
                && c.DistanceTo(gate.Position) >= 3f,
            null,
            out near);

        if (!found)
            near = CellFinderLoose.RandomCellWith(c => c.Standable(map), map);

        var dhdDef = RimgateDefOf.Rimgate_DialHomeDevice;
        var dhd = GenSpawn.Spawn(dhdDef, near, map, Rot4.North, WipeMode.Vanish);
        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Spawned a fallback DHD at {near} on {map}.");
    }
}
