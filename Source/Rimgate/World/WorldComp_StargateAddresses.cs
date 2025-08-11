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
    private const string StargateTag = "Rimgate_StargateSite";

    private List<PlanetTile> _addressList = new();

    public List<PlanetTile> AddressList => _addressList;

    public int AddressCount => _addressList.Count;

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
            WorldObject_PermanentStargateSite => true,
            Site s when s.MainSitePartDef?.tags?.Contains(StargateTag) == true => true,
            _ => false
        };
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref _addressList, "_addressList");
    }
}
