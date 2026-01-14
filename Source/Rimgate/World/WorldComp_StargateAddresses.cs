using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;

namespace Rimgate;

public class WorldComp_StargateAddresses : WorldComponent
{
    public List<PlanetTile> AddressList => _addressList;

    public int AddressCount => _addressList.Count;

    public bool ModificationEquipmentActive;

    private List<PlanetTile> _addressList = new();

    public WorldComp_StargateAddresses(World world) : base(world) { }

    public void AddAddress(PlanetTile address)
    {
        if (!_addressList.Contains(address))
            _addressList.Add(address);
    }

    public void RemoveAddress(PlanetTile address)
    {
        _addressList.Remove(address);
    }

    public void CleanupAddresses()
    {
        _addressList.RemoveAll(tile => !IsValidAddress(tile));
    }

    private static bool IsValidAddress(PlanetTile address)
    {
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
    private static bool SiteHasPlayerPresence(Site site)
    {
        if (site == null || !site.HasMap)
            return false;

        Map map = site.Map;
        return map != null && map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Any();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref _addressList, "_addressList");
        Scribe_Values.Look(ref ModificationEquipmentActive, "ModificationEquipmentActive");
    }
}

