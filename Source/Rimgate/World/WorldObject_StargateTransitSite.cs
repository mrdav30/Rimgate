using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Pool;
using Verse;

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
        ? Building_DHD.GetDhdOfOnMap(Map, RimgateDefOf.Rimgate_DialHomeDevice) != null
        : _lastKnownHasDhd;

    private const float LootChanceItems = 0.35f;

    private List<TransitStoredThing> _storedThings = new();

    private bool _lastKnownHasGate;

    private bool _lastKnownHasDhd;

    private bool _ranInitialSetup;

    private bool _wasLooted;

    private static bool IsInfraDef(ThingDef def) =>
    def == RimgateDefOf.Rimgate_Stargate || def == RimgateDefOf.Rimgate_DialHomeDevice;

    public override string GetInspectString()
    {
        // Show address + compact loadout state
        var address = StargateUtil.GetStargateDesignation(Tile);
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
        var gateOnMap = Building_Stargate.GetStargateOnMap(Map);
        var dhdOnMap = Building_DHD.GetDhdOfOnMap(Map, RimgateDefOf.Rimgate_DialHomeDevice);

        // Cache last known presence
        _lastKnownHasGate = gateOnMap != null;
        _lastKnownHasDhd = dhdOnMap != null;

        // If the map is eligible to despawn, snapshot player things
        bool canDespawn = !StargateUtil.ActiveGateOnMap(Map) && !Map.mapPawns.AnyPawnBlockingMapRemoval;

        if (canDespawn)
        {
            SnapshotPlayerThings(Map);

            if (_storedThings.Count > 0)
            {
                int removed = _storedThings.RemoveAll(s => !IsInfraDef(s.Def) && Rand.Value < LootChanceItems);

                if (removed > 0)
                    _wasLooted = true;
            }
        }

        // remove the world object only when BOTH are gone
        alsoRemoveWorldObject = gateOnMap == null && dhdOnMap == null;

        return canDespawn;
    }

    private void SnapshotPlayerThings(Map map)
    {
        _storedThings.Clear();

        // As we loop, keep "last known" flags in sync post-setup
        bool sawGate = false, sawDhd = false;

        foreach (var t in map.listerThings.AllThings)
        {
            if (t == null || !t.Spawned) continue;

            // Track infra presence
            if (t is Building_Stargate) sawGate = true;
            if (t is Building_DHD) sawDhd = true;

            if (!t.Faction.IsOfPlayerFaction()) continue;

            // Skip pawns/filth/frames/blueprints/plants/corpses/chunks
            if (t is Pawn || t is Blueprint || t is Frame || t.def.category == ThingCategory.Plant) continue;
            if (t.def.IsFilth || t.def.IsFrame || t.def.IsBlueprint) continue;
            if (t.def.mineable || t.def.building?.isNaturalRock == true) continue;
            if (t is Corpse) continue;

            _storedThings.Add(new TransitStoredThing(t));
        }

        _lastKnownHasGate = sawGate;
        _lastKnownHasDhd = sawDhd;
    }

    public override void PostMapGenerate()
    {
        base.PostMapGenerate();

        // Clear hostiles
        var toWipe = Map.mapPawns.AllPawnsSpawned
            .Where(p => !p.Faction.IsOfPlayerFaction() && p.HostileTo(Faction.OfPlayer))
            .ToList();
        foreach (var p in toWipe) p.Destroy();

        // Salvage/place infra according to initial loadout
        ValidateGateOnMap();

        // Restore cached things (near gate if possible)
        RestoreCachedThings();

        // Inform the player if scavengers helped themselves while they were gone.
        if (_wasLooted)
        {
            Find.LetterStack.ReceiveLetter(
                "RG_LetterTransitSiteLootedLabel".Translate(),
                "RG_LetterTransitSiteLootedText".Translate(Label),
                LetterDefOf.NegativeEvent,
                new LookTargets(Map.Center, Map));

            _wasLooted = false;
        }

        RefreshPresenceCache();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitState(bool hasGate, bool hasDhd)
    {
        InitialHadGate = _lastKnownHasGate = hasGate;
        InitialHadDhd = _lastKnownHasDhd = hasDhd;
    }

    private void ValidateGateOnMap()
    {
        if (_ranInitialSetup || !InitialHadGate)
            return;

        var gateOnMap = Building_Stargate.GetStargateOnMap(Map);
        if (gateOnMap == null)
            gateOnMap = StargateUtil.PlaceRandomGate(Map, Faction.OfPlayer);
        else
            gateOnMap.SetFaction(Faction.OfPlayer);

        // Only ensure a DHD if the site was created with one
        if (InitialHadDhd)
            StargateUtil.EnsureDhdNearGate(Map, gateOnMap, Faction.OfPlayer);

        _ranInitialSetup = true;
    }

    private void RestoreCachedThings()
    {
        if (_storedThings == null || _storedThings.Count == 0) return;

        foreach (var rec in _storedThings)
        {
            if (rec.Def == null) continue;

            var thing = TransitStoredThing.MakeThingFromRecord(rec);
            if (thing == null) continue;

            // Player ownership for anything we rebuild
            thing.SetFaction(Faction.OfPlayer);

            // Place at saved cell/rot if valid; else fallback near center
            Utils.TryPlaceExactOrNear(thing, Map, rec.Pos, rec.Rot);
        }

        _storedThings.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RefreshPresenceCache()
    {
        if (Map == null) return;
        _lastKnownHasGate = Building_Stargate.GetStargateOnMap(Map) != null;
        _lastKnownHasDhd = Building_DHD.GetDhdOfOnMap(Map, RimgateDefOf.Rimgate_DialHomeDevice) != null;
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
        Scribe_Values.Look(ref _ranInitialSetup, "_ranInitialSetup", false);
        Scribe_Collections.Look(ref _storedThings, "_storedThings", LookMode.Deep);
        Scribe_Values.Look(ref _wasLooted, "_wasLooted", false);
    }
}
