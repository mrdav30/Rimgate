using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace Rimgate;

public class WorldObject_GateTransitSite : MapParent, IRenameable, IThingHolder
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
        ? Building_Gate.TryGetSpawnedGateOnMap(Map, out _)
        : _lastKnownHasGate;

    public bool HasDhd => Map != null
        ? Building_DHD.TryGetDhdOnMap(Map, out _, RimgateDefOf.Rimgate_DialHomeDevice)
        : _lastKnownHasDhd;

    private const float LootChanceItems = 0.0035f;

    private List<TransitStoredThing> _storedThings = new();

    // Value records cannot represent an actual corpse's pawn, health and inventory.
    // These remains stay deep-owned intact; the existing item-scavenging rule applies
    // only to the ordinary value records, not to newly preserved pawn/corpse state.
    private ThingOwner<Corpse> _storedCorpses;

    private List<TransitStoredCorpseContainer> _storedCorpseContainers = new();

    public WorldObject_GateTransitSite()
    {
        _storedCorpses = new ThingOwner<Corpse>(this);
    }

    ThingOwner IThingHolder.GetDirectlyHeldThings() => _storedCorpses;

    public override void GetChildHolders(List<IThingHolder> outChildren)
    {
        base.GetChildHolders(outChildren);
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, _storedCorpses);
        if (_storedCorpseContainers != null)
            outChildren.AddRange(_storedCorpseContainers);
    }

    private bool _lastKnownHasGate;

    private bool _lastKnownHasDhd;

    private bool _ranInitialSetup;

    private bool _wasLooted;

    // anything else can be removed by scavengers
    private static bool IsImportant(ThingDef def) => def == RimgateDefOf.Rimgate_Dwarfgate
        || def == RimgateDefOf.Rimgate_DialHomeDevice;

    public override void SpawnSetup()
    {
        // register gate address for this site since gate is placed during mapgen
        GateUtil.AddGateAddress(Tile);
    }

    public override string GetInspectString()
    {
        // Show address + compact loadout state
        var address = GateUtil.GetGateDesignation(Tile);
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

    public override void Notify_MyMapAboutToBeRemoved()
    {
        if (Building_Gate.TryGetSpawnedGateOnMap(Map, out Building_Gate gate))
            gate.CloseGate(gate.ConnectedGate != null);
        base.Notify_MyMapAboutToBeRemoved();
    }

    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        // Cache last known presence
        _lastKnownHasGate = Building_Gate.TryGetSpawnedGateOnMap(Map, out Building_Gate gateOnMap);
        _lastKnownHasDhd = Building_DHD.TryGetDhdOnMap(Map, out Building_DHD dhdOnMap, RimgateDefOf.Rimgate_DialHomeDevice);

        // Do not take ownership away from a live map before its final removal veto.
        if (TransporterUtility.IncomingTransporterPreventingMapRemoval(base.Map))
        {
            alsoRemoveWorldObject = false;
            return false;
        }

        // If the map is eligible to despawn, snapshot player things and loose cargo.
        bool canDespawn = gateOnMap?.IsActive != true && !Map.mapPawns.AnyPawnBlockingMapRemoval;
        if (canDespawn)
        {
            SnapshotPlayerThings(Map);

            if (_storedThings.Count > 0)
            {
                int removed = _storedThings.RemoveAll(s => !IsImportant(s.Def) && Rand.Chance(LootChanceItems));

                if (removed > 0)
                    _wasLooted = true;
            }

            foreach (var entry in _storedCorpseContainers.ToArray())
            {
                if (entry.Root == null || IsImportant(entry.Root.def) || !Rand.Chance(LootChanceItems)) continue;
                // Scavenge containers at the existing rate, but retain their actual bodies.
                bool retainedBodies = true;
                foreach (var corpse in FindContainedCorpses(entry.Root))
                {
                    if (corpse.holdingOwner?.TryTransferToContainer(corpse, _storedCorpses, 1) == 1) continue;
                    retainedBodies = false;
                    break;
                }
                if (!retainedBodies) continue;
                entry.DestroyContents();
                _storedCorpseContainers.Remove(entry);
                _wasLooted = true;
            }
        }

        // Keep the site's retained cargo reachable even if both pieces of infra are gone.
        alsoRemoveWorldObject = gateOnMap == null && dhdOnMap == null
            && _storedThings.Count == 0 && _storedCorpses.Count == 0
            && _storedCorpseContainers.Count == 0;
        return canDespawn;
    }

    private void SnapshotPlayerThings(Map map)
    {
        // Any records left after regeneration are failed placements, not live map
        // copies. Retain them when this map is removed again.

        // As we loop, keep "last known" flags in sync post-setup
        bool sawGate = false, sawDhd = false;

        foreach (var t in map.listerThings.AllThings.ToArray())
        {
            if (t == null || !t.Spawned) continue;

            // Track infra presence
            if (t is Building_Gate) sawGate = true;
            if (t is Building_DHD) sawDhd = true;

            if (!t.Faction.IsOfPlayerFaction()
                && !(t.Faction == null && t.def.category == ThingCategory.Item)) continue;

            if (t is Corpse corpse)
            {
                corpse.DeSpawn();
                if (!_storedCorpses.TryAdd(corpse, false))
                    throw new System.InvalidOperationException("Could not retain transit-site corpse ownership.");
                continue;
            }

            // Skip pawns/filth/frames/blueprints/plants/chunks.
            if (t is Pawn || t is Blueprint || t is Frame || t.def.category == ThingCategory.Plant) continue;
            if (t.def.IsFilth || t.def.IsFrame || t.def.IsBlueprint) continue;
            if (t.def.mineable || t.def.building?.isNaturalRock == true) continue;
            // Natural stone is map context, not retained cargo. Do not exclude all
            // Chunks: the transported ReplicatorLink resource belongs to that category.
            if (t.def.IsWithinCategory(ThingCategoryDefOf.StoneChunks)) continue;
            if ((t is Building_MobileContainer || t is MinifiedThing { InnerThing: Building_MobileContainer })
                && FindContainedCorpses(t).Count > 0)
            {
                _storedCorpseContainers.Add(new TransitStoredCorpseContainer(this, t));
                continue;
            }
            _storedThings.Add(new TransitStoredThing(t));
        }

        _lastKnownHasGate = sawGate;
        _lastKnownHasDhd = sawDhd;
    }

    public override void Destroy()
    {
        GateUtil.RemoveGateAddress(Tile);
        base.Destroy();
        _storedCorpses.ClearAndDestroyContents();
        foreach (var entry in _storedCorpseContainers) entry.DestroyContents();
        _storedCorpseContainers.Clear();
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

        var map = Map;
        if (!Building_Gate.TryGetSpawnedGateOnMap(map, out Building_Gate gateOnMap))
            gateOnMap = GateUtil.PlaceRandomGate(map, Faction.OfPlayer);
        else
            gateOnMap.SetFaction(Faction.OfPlayer);

        // Only ensure a DHD if the site was created with one
        if (InitialHadDhd)
            GateUtil.EnsureDhdNearGate(map, gateOnMap, Faction.OfPlayer);

        _ranInitialSetup = true;
    }

    private void RestoreCachedThings()
    {
        // Restore infrastructure before choosing item cells, so exclusions use the
        // actual gates and their rotated authored vortex patterns.
        foreach (var rec in _storedThings.OrderBy(r => r.Def?.category == ThingCategory.Item).ToArray())
        {
            if (rec.Def == null) continue;

            var thing = TransitStoredThing.MakeThingFromRecord(rec);
            if (thing == null) continue;

            // Player ownership for anything we rebuild
            if (thing.def.CanHaveFaction)
                thing.SetFaction(Faction.OfPlayer);

            bool placed;
            if (thing.def.category == ThingCategory.Item)
            {
                var rotation = rec.Rot ?? thing.def.defaultPlacingRot;
                placed = rec.Pos.HasValue && CanRestoreItemAt(thing, rec.Pos.Value, rotation)
                    && GenPlace.TryPlaceThing(thing, rec.Pos.Value, Map, ThingPlaceMode.Direct, rot: rotation);
                if (!placed)
                    placed = GenPlace.TryPlaceThing(thing, RestorationAnchor(), Map, ThingPlaceMode.Near,
                        extraValidator: cell => CanRestoreItemAt(thing, cell, rotation), rot: rotation);
            }
            else
            {
                Utils.TryPlaceExactOrNear(thing, Map, rec.Pos, rec.Rot);
                placed = thing.Spawned;
            }

            if (placed)
                _storedThings.Remove(rec);
            else
                thing.Destroy(DestroyMode.Vanish); // Keep its value record for a later regeneration.
        }

        foreach (var entry in _storedCorpseContainers.ToArray())
        {
            var root = entry.Root;
            if (root == null) { _storedCorpseContainers.Remove(entry); continue; }
            bool placed = CanRestoreItemAt(root, entry.Position, entry.Rotation)
                && GenPlace.TryPlaceThing(root, entry.Position, Map, ThingPlaceMode.Direct, rot: entry.Rotation);
            if (!placed)
                placed = GenPlace.TryPlaceThing(root, RestorationAnchor(), Map, ThingPlaceMode.Near,
                    extraValidator: cell => CanRestoreItemAt(root, cell, entry.Rotation), rot: entry.Rotation);
            if (placed)
            {
                entry.Contents.Remove(root);
                _storedCorpseContainers.Remove(entry);
            }
        }

        foreach (var corpse in _storedCorpses.InnerListForReading.ToArray())
        {
            // Native Near placement handles corpses; reject wiping and full item cells
            // explicitly so a failed restoration leaves this exact body in its owner.
            _storedCorpses.TryDrop(corpse, RestorationAnchor(), Map, ThingPlaceMode.Near, out _,
                nearPlaceValidator: cell => CanRestoreItemAt(corpse, cell, corpse.Rotation),
                playDropSound: false);
        }
    }

    private IntVec3 RestorationAnchor() => Building_Gate.TryGetSpawnedGateOnMap(Map, out var gate)
        ? gate.Position : Map.Center;

    private static List<Corpse> FindContainedCorpses(Thing root)
    {
        var corpses = new List<Corpse>();
        var visited = new HashSet<IThingHolder>(ReferenceEqualityComparer.Instance);
        void Visit(IThingHolder holder)
        {
            if (!visited.Add(holder)) return;
            if (holder.GetDirectlyHeldThings() is { } owner)
                foreach (var thing in owner)
                    if (thing is Corpse corpse) corpses.Add(corpse);
            var children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            foreach (var child in children) Visit(child);
        }
        if (root is IThingHolder holder) Visit(holder);
        return corpses;
    }

    private bool CanRestoreItemAt(Thing thing, IntVec3 cell, Rot4 rotation) =>
        GenSpawn.CanSpawnAt(thing.def, cell, Map, rotation, false)
        && GenAdj.OccupiedRect(cell, rotation, thing.def.size).All(c => c.InBounds(Map)
            && c.GetItemCount(Map) < c.GetMaxItemsAllowedInCell(Map)
            && !c.GetThingList(Map).Any(t => GenSpawn.SpawningWipes(thing.def, t.def, false))
            && !Map.listerThings.AllThings.OfType<Building_Gate>().Any(g => g.VortexCells.Contains(c)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RefreshPresenceCache()
    {
        if (Map == null) return;
        _lastKnownHasGate = Building_Gate.TryGetSpawnedGateOnMap(Map, out _);
        _lastKnownHasDhd = Building_DHD.TryGetDhdOnMap(Map, out _, RimgateDefOf.Rimgate_DialHomeDevice);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;

        yield return new Command_Action
        {
            icon = RimgateTex.RenameCommandTex,
            action = () => { Find.WindowStack.Add(new Dialog_RenameGateSite(this)); },
            defaultLabel = "RG_RenameTransitGateSite".Translate(),
            defaultDesc = "RG_RenameTransitGateSiteDesc".Translate()
        };
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
    {
        return CaravanArrivalActionUtility.GetFloatMenuOptions(
            () => true,
            () => new CaravanArrivalAction_PermanentGateSite(this),
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
        Scribe_Deep.Look(ref _storedCorpses, "_storedCorpses", this);
        Scribe_Collections.Look(ref _storedCorpseContainers, "_storedCorpseContainers", LookMode.Deep, this);
        Scribe_Values.Look(ref _wasLooted, "_wasLooted", false);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            _storedThings ??= new List<TransitStoredThing>();
            _storedCorpses ??= new ThingOwner<Corpse>(this);
            _storedCorpseContainers ??= new List<TransitStoredCorpseContainer>();
        }
    }
}
