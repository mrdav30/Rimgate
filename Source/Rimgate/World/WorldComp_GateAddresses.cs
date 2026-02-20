using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace Rimgate;

public class WorldComp_GateAddresses : WorldComponent
{
    public List<PlanetTile> AddressList = new(GateUtil.MaxAddresses);

    public bool ModificationEquipmentActive;

    public int ActiveQuestSiteCount;

    public WorldComp_GateAddresses(World world) : base(world) { }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref AddressList, "AddressList");
        Scribe_Values.Look(ref ModificationEquipmentActive, "ModificationEquipmentActive");
        Scribe_Values.Look(ref ActiveQuestSiteCount, "ActiveQuestSiteCount", 0);
    }
}

