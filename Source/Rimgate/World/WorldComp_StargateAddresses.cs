using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Tilemaps;
using Verse;

namespace Rimgate;

public class WorldComp_StargateAddresses : WorldComponent
{
    public List<PlanetTile> AddressList = new(StargateUtil.MaxAddresses);

    public bool ModificationEquipmentActive;

    public int ActiveQuestSiteCount;

    public WorldComp_StargateAddresses(World world) : base(world) { }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref AddressList, "AddressList");
        Scribe_Values.Look(ref ModificationEquipmentActive, "ModificationEquipmentActive");
        Scribe_Values.Look(ref ActiveQuestSiteCount, "ActiveQuestSiteCount", 0);
    }
}

