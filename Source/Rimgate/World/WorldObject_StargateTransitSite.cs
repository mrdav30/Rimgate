using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rimgate;

public class WorldObject_StargateTransitSite : MapParent, IRenameable
{
    public string SiteName;

    public bool InitialHadGate = true;

    public bool InitialHadDhd = false;

    public override string Label => SiteName ?? base.Label;
    
    public string RenamableLabel
    {
        get => Label;
        set => SiteName = value;
    }

    public string BaseLabel => Label;

    public string InspectLabel => Label;

    public bool HasGate => Map != null
    ? Building_Stargate.GetStargateOnMap(Map) != null
    : _lastKnownHasGate;

    public bool HasDhd => Map != null
        ? Building_DHD.GetDhdOnMap(Map) != null
        : _lastKnownHasDhd;

    private bool _lastKnownHasGate;

    private bool _lastKnownHasDhd;

    public override string GetInspectString()
    {
        // Show address + compact loadout state
        var address = StargateUtility.GetStargateDesignation(Tile);
        var gateToken = HasGate 
            ? "RG_SiteLoadout_Gate".Translate() 
            : "RG_SiteLoadout_NoGate".Translate();
        var dhdToken = HasDhd 
            ? "RG_SiteLoadout_DHD".Translate() 
            : "RG_SiteLoadout_NoDHD".Translate();
        var state = $"{gateToken} / {dhdToken}";

        return "RG_GateAddress".Translate(address) + "\n"
            + "RG_SiteLoadout".Translate(state);
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
        // remove the world object only when BOTH are gone
        alsoRemoveWorldObject = gateOnMap == null 
            && dhdOnMap == null;

        _lastKnownHasGate = gateOnMap != null;
        _lastKnownHasDhd = dhdOnMap?.def == RimgateDefOf.Rimgate_DialHomeDevice;

        return !StargateUtility.ActiveGateOnMap(Map)
            && !Map.mapPawns.AnyPawnBlockingMapRemoval;
    }

    public override void PostMapGenerate()
    {
        base.PostMapGenerate();

        // 1) Clear hostiles (keep friendlies/neutral pawns)
        var toWipe = Map.mapPawns.AllPawnsSpawned
            .Where(p => p.Faction != Faction.OfPlayer && p.HostileTo(Faction.OfPlayer))
            .ToList();
        foreach (var p in toWipe) p.Destroy();

        var gateOnMap = Building_Stargate.GetStargateOnMap(Map);
        if (InitialHadGate)
        {
            if (gateOnMap == null)
            {
                var newGate = StargateUtility.PlaceRandomGateAndDHD(
                    Map,
                    Faction.OfPlayer);
                if (!InitialHadDhd)
                {
                    // Player created a gate-only site;
                    // remove any helper DHD that placer might have added.
                    var dhd = Building_DHD.GetDhdOnMap(Map);
                    dhd?.Destroy(DestroyMode.Vanish);
                }
            }
            else
            {
                gateOnMap.SetFaction(Faction.OfPlayer);
                if (InitialHadDhd)
                {
                    StargateUtility.EnsureDhdNearGate(Map, gateOnMap, Faction.OfPlayer);
                }
                else
                {
                    // If a DHD happens to be present, just claim it;
                    // do not create one.
                    var existingDhd = Building_DHD.GetDhdOnMap(Map);
                    existingDhd?.SetFaction(Faction.OfPlayer);
                }
            }
        }

        RefreshPresenceCache();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitState(bool hasGate, bool hasDhd)
    {
        InitialHadGate = _lastKnownHasGate = hasGate;
        InitialHadDhd = _lastKnownHasDhd = hasDhd;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RefreshPresenceCache()
    {
        if (Map == null) return;
        _lastKnownHasGate = Building_Stargate.GetStargateOnMap(Map) != null;
        var dhd = Building_DHD.GetDhdOnMap(Map);
        _lastKnownHasDhd = dhd?.def == RimgateDefOf.Rimgate_DialHomeDevice;
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
        Scribe_Values.Look(ref InitialHadGate, "InitialHadGate");
        Scribe_Values.Look(ref InitialHadDhd, "InitialHadDhd");
        Scribe_Values.Look(ref _lastKnownHasGate, "_lastKnownHasGate");
        Scribe_Values.Look(ref _lastKnownHasDhd, "_lastKnownHasDhd");
    }
}
