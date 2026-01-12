using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Rimgate;

public static class StargateUtil
{
    public const string Alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static WorldComp_StargateAddresses WorldComp => _cachedWorldComp ??= Find.World.GetComponent<WorldComp_StargateAddresses>();

    private const int MaxGateSearchRadius = 50;

    private const int MaxTries = 2000;

    private static WorldComp_StargateAddresses _cachedWorldComp;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetActiveGateOnMap(Map map, out Building_Stargate gate)
    {
        gate = Building_Stargate.GetStargateOnMap(map);
        return gate?.IsActive == true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanEnterGate(Pawn pawn, Building_Stargate gate)
    {
        return pawn.CanReach(gate, PathEndMode.ClosestTouch, Danger.Deadly);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddGateAddress(PlanetTile address)
    {
        if (WorldComp != null)
            WorldComp.AddAddress(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveGateAddress(PlanetTile address)
    {
        if (WorldComp != null)
            WorldComp.RemoveAddress(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ModificationEquipmentActive() => WorldComp?.ModificationEquipmentActive == true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetModificationEquipmentActive(bool status)
    {
        if (WorldComp != null)
            WorldComp.ModificationEquipmentActive = status;
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

    public static Building_Stargate PlaceRandomGate(Map map, Faction faction = null)
    {
        // Find a safe spot for a new gate
        // (near-ish center, unfogged, standable, no edifice, not roofed)
        var center = map.Center;
        var safe = CellFinder.TryFindRandomCellNear(
            center,
            map,
            MaxGateSearchRadius,
            c => c.InBounds(map)
                && c.Standable(map)
                && c.SupportsStructureType(map, TerrainAffordanceDefOf.Heavy)
                && !c.Fogged(map)
                && !c.Roofed(map)
                && c.GetEdifice(map) == null
                && map.reachability.CanReach(c, center, PathEndMode.OnCell, TraverseMode.PassDoors, Danger.Deadly),
            out IntVec3 spot);

        if (!safe)
        {
            spot = CellFinderLoose.RandomCellWith(c => c.Standable(map), map);

            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: Unable to find a safe spot to spawn a gate, spawning randomly at {spot}.");
        }

        var gate = GenSpawn.Spawn(
            RimgateDefOf.Rimgate_Dwarfgate,
            spot,
            map,
            Rot4.North,
            WipeMode.Vanish) as Building_Stargate;

        if (faction != null) gate?.SetFaction(faction);

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Spawned a fallback Stargate at {spot} on {map}.");

        return gate;
    }

    public static void EnsureDhdNearGate(Map map, Building_Stargate gate, Faction faction = null)
    {
        // Any DHD within ~8 tiles (Facility default) is fine
        bool HasLinkedDhdNearby()
        {
            return map.listerThings.AllThings.Any(t =>
                t is Building_DHD
                && t.Spawned
                && t.Position.InHorDistOf(gate.Position, 8f));
        }

        if (gate == null || HasLinkedDhdNearby())
            return;

        // Find a near spot (4..8 tiles) that’s clean for a 1x1 DHD
        IntVec3 near;
        bool found = CellFinder.TryFindRandomReachableNearbyCell(
            gate.Position,
            map,
            6f,
            TraverseParms.For(TraverseMode.PassDoors),
            c => c.InBounds(map)
                && c.Standable(map)
                && c.SupportsStructureType(map, TerrainAffordanceDefOf.Heavy)
                && !c.Roofed(map)
                && !c.Fogged(map)
                && c.GetEdifice(map) == null
                && c.DistanceTo(gate.Position) >= 3f,
            null,
            out near);

        if (!found)
        {
            near = CellFinderLoose.RandomCellWith(c => c.Standable(map), map);

            if (RimgateMod.Debug)
                Log.Message($"Rimgate :: Unable to find a safe spot to spawn a dhd, spawning randomly at {near}.");
        }

        var dhdDef = RimgateDefOf.Rimgate_DialHomeDevice;
        var dhd = GenSpawn.Spawn(
            dhdDef,
            near, map,
            Rot4.North,
            WipeMode.Vanish);

        if (faction != null) dhd?.SetFaction(faction);

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Spawned a fallback DHD at {near} on {map}.");
    }
}
