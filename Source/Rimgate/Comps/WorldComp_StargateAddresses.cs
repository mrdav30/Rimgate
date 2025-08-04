using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rimgate;

public class WorldComp_StargateAddresses : WorldComponent
{
    public List<PlanetTile> AddressList = new List<PlanetTile>();

    public WorldComp_StargateAddresses(World world) : base(world) { }

    public void RemoveAddress(PlanetTile address)
    {
        AddressList.Remove(address);
    }

    public void AddAddress(PlanetTile address)
    {
        if (!AddressList.Contains(address))
            AddressList.Add(address);
    }

    public void CleanupAddresses()
    {
        foreach (var tile in new List<PlanetTile>(AddressList))
        {
            MapParent sgMap = Find.WorldObjects.MapParentAt(tile);
            Site site = sgMap as Site;

            if (sgMap == null || sgMap.HasMap)
                continue;

            bool noGate = sgMap == null 
                || !sgMap.HasMap
                    && sgMap is not WorldObject_PermanentStargateSite
                    && (site == null 
                        || !site.MainSitePartDef.tags.Contains("Rimgate_StargateSite"));
            if (noGate)
                RemoveAddress(tile);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref AddressList, "AddressList");
    }
}
