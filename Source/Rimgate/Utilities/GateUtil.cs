using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimgate;

public static class GateUtil
{
    // TODO: move this to mod config
    public const int MaxAddresses = 11;

    public const int MaxActiveQuestSiteCount = 2;

    public const string Alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private const int MaxGateSearchRadius = 50;

    private const int MaxTries = 2000;

    public static WorldComp_GateAddresses WorldComp => _cachedWorldComp ??= Find.World.GetComponent<WorldComp_GateAddresses>();

    private static WorldComp_GateAddresses _cachedWorldComp;

    public static bool ModificationEquipmentActive => WorldComp?.ModificationEquipmentActive == true;

    public static int AddressCount => WorldComp?.AddressList.Count ?? 0;

    public static bool AddressBookFull => AddressCount >= MaxAddresses;

    public static int ActiveQuestSiteCount => WorldComp?.ActiveQuestSiteCount ?? 0;

    public static bool ActiveQuestSitesAtLimit => ActiveQuestSiteCount >= MaxActiveQuestSiteCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetActiveGateOnMap(Map map, out Building_Gate gate) => 
        Building_Gate.TryGetSpawnedGateOnMap(map, out gate) 
        && gate.IsActive;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddGateAddress(PlanetTile address)
    {
        bool valid = WorldComp != null
            && !AddressBookFull
            && address.Valid
            && !WorldComp.AddressList.Contains(address);
        if (valid)
            WorldComp.AddressList.Add(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveGateAddress(PlanetTile address)
    {
        if (WorldComp != null)
            WorldComp.AddressList.Remove(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CleanupAddresses()
    {
        WorldComp.AddressList.RemoveAll(tile =>
        {
            if (!IsValidAddress(tile))
            {
                if (RimgateMod.Debug)
                    Log.Message($"Rimgate :: Gate Address Cleanup: Removing invalid address at tile {tile}");
                return true;
            }
            return false;
        });
    }

    public static List<PlanetTile> GetAddressList(PlanetTile exclude = default)
    {
        if (WorldComp == null)
            return new List<PlanetTile>();

        GateUtil.CleanupAddresses();
        return WorldComp.AddressList
            .Where(tile => tile != exclude)
            .ToList();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IncrementQuestSiteCount()
    {
        if (WorldComp != null)
            WorldComp.ActiveQuestSiteCount = Mathf.Min(WorldComp.ActiveQuestSiteCount + 1, MaxActiveQuestSiteCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DecrementQuestSiteCount()
    {
        if (WorldComp != null)
            WorldComp.ActiveQuestSiteCount = Mathf.Max(WorldComp.ActiveQuestSiteCount - 1, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetModificationEquipmentActive(bool status)
    {
        if (WorldComp != null)
            WorldComp.ModificationEquipmentActive = status;
    }

    public static bool IsValidAddress(PlanetTile address)
    {
        if(!address.Valid)
            return false;

        var mp = Find.WorldObjects.MapParentAt(address);
        return mp switch
        {
            null => false,
            { HasMap: true } => true,
            WorldObject_GateTransitSite => true,
            WorldObject_GateQuestSite => true,
            Site s when SiteHasPlayerPresence(s) => true,
            _ => false
        };
    }

    public static bool SiteHasPlayerPresence(Site site)
    {
        if (site == null || !site.HasMap)
            return false;

        Map map = site.Map;
        return map != null && map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Any();
    }

    public static string GetGateDesignation(PlanetTile address)
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

    public static Building_Gate PlaceRandomGate(Map map, Faction faction = null)
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
            WipeMode.Vanish) as Building_Gate;

        if (faction != null) gate?.SetFaction(faction);

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Spawned a fallback gate at {spot} on {map}{(faction != null ? $" for faction {faction}" : "")}.");

        return gate;
    }

    public static void EnsureDhdNearGate(Map map, Building_Gate gate, Faction faction = null)
    {
        // Any DHD within ~8 tiles (Facility default) is fine
        bool HasLinkedDhdNearby()
        {
            return map.listerThings.AllThings.Any(t =>
                t is Building_DHD
                && t.Spawned
                && t.Position.InHorDistOf(gate.Position, 7f));
        }

        if (gate == null || HasLinkedDhdNearby())
            return;

        // Find a near spot (4..8 tiles) that’s clear for a DHD
        IntVec3 near;
        bool found = CellFinder.TryFindRandomReachableNearbyCell(
            gate.Position,
            map,
            7f,
            TraverseParms.For(TraverseMode.PassDoors),
            c => c.InBounds(map)
                && c.Standable(map)
                && c.SupportsStructureType(map, TerrainAffordanceDefOf.Heavy)
                && !c.Roofed(map)
                && !c.Fogged(map)
                && c.GetEdifice(map) == null
                && c.DistanceTo(gate.Position) > 4f,
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
            near,
            map,
            Rot4.North,
            WipeMode.Vanish);

        if (faction != null) dhd?.SetFaction(faction);

        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: Spawned a fallback DHD at {near} on {map}{(faction != null ? $" for faction {faction}" : "")}.");
    }
}
