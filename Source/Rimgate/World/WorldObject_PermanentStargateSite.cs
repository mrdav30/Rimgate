using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using System.Collections.Generic;

namespace Rimgate;

public class WorldObject_PermanentStargateSite : MapParent, IRenameable
{
    public string SiteName;

    public override string Label => SiteName ?? base.Label;

    public string RenamableLabel
    {
        get => Label;
        set => SiteName = value;
    }

    public string BaseLabel => Label;

    public string InspectLabel => Label;

    public override string GetInspectString()
    {
        return "RG_GateAddress".Translate(StargateUtility.GetStargateDesignation(Tile));
    }

    public override void SpawnSetup()
    {
        base.SpawnSetup();
        Find.World.GetComponent<WorldComp_StargateAddresses>().AddAddress(Tile);
    }

    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        Building_Stargate gateOnMap = Building_Stargate.GetStargateOnMap(Map);
        Building_DHD dhdOnMap = Building_DHD.GetDhdOnMap(Map);
        alsoRemoveWorldObject = gateOnMap == null && dhdOnMap == null;
        return !StargateUtility.ActiveGateOnMap(Map)
            && !Map.mapPawns.AnyPawnBlockingMapRemoval;
    }

    public override void PostMapGenerate()
    {
        base.PostMapGenerate();

        var pawns = Map.mapPawns.AllPawns
            .Where(p => p.RaceProps.Humanlike || p.HostileTo(Faction.OfPlayer))
            .ToList();
        foreach (var pawn in pawns)
            pawn.Destroy();

        Building_Stargate gateOnMap = Building_Stargate.GetStargateOnMap(Map);
        Building_DHD dhdOnMap = Building_DHD.GetDhdOnMap(Map);
        if (RimgateMod.Debug)
            Log.Message($"Rimgate :: perm sg site post map gen: gateonmap={gateOnMap} dhdonmap={dhdOnMap}");

        if (gateOnMap != null)
        {
            IntVec3 gatePos = gateOnMap.Position;
            gateOnMap.Destroy();
            var spawnedSG = GenSpawn.Spawn(RimgateDefOf.Rimgate_Stargate, gatePos, Map);
            spawnedSG.SetFaction(Faction.OfPlayer);
        }

        if (dhdOnMap != null)
        {
            IntVec3 dhdPos = dhdOnMap.Position;
            dhdOnMap.Destroy();
                var spawnedDHD = GenSpawn.Spawn(RimgateDefOf.Rimgate_DialHomeDevice, dhdPos, Map);
                spawnedDHD.SetFaction(Faction.OfPlayer);
        }
    }

    public override void Notify_MyMapRemoved(Map map)
    {
        base.Notify_MyMapRemoved(map);
        Building_Stargate gateOnMap = Building_Stargate.GetStargateOnMap(Map);
        Building_DHD dhdOnMap = Building_DHD.GetDhdOnMap(Map);
        if (gateOnMap == null && dhdOnMap == null)
            Destroy();
    }

    public override void Destroy()
    {
        base.Destroy();
        Find.World.GetComponent<WorldComp_StargateAddresses>().RemoveAddress(Tile);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;

        yield return new Command_Action
        {
            icon = RimgateTex.RenameCommandTex,
            action = () => { Find.WindowStack.Add(new Dialog_RenameStargateSite(this)); },
            defaultLabel = "RG_RenameGateSite".Translate(),
            defaultDesc = "RG_RenameGateSiteDesc".Translate()
        };
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
    {
        return CaravanArrivalActionUtility.GetFloatMenuOptions(
            () => true,
            () => new CaravanArrivalAction_PermanentStargateSite(this),
            $"Approach {Label}",
            caravan,
            Tile,
            this);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref SiteName, "SiteName");
    }
}
