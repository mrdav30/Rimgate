using System.Collections;
using System.Reflection;
using RimBridgeServer.Sdk;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Rimgate.AssetProbe;

// Fixture preparation and observation only; dialing and transfer remain production jobs.
public sealed class TransitProbe
{
    private const string DestinationName = "Rimgate transit destination";
    private static readonly FieldInfo SendBuffer = typeof(Building_Gate).GetField("_sendBuffer", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo ReceiveBuffer = typeof(Building_Gate).GetField("_recvBuffer", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo PendingOwner = typeof(Building_Gate).GetField("_pendingContents", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo RedraftIds = typeof(Building_Gate).GetField("_redraftOnArrival", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Tool("rimgate/prepare_transit_fixture", Description = "Prepare a lazy transit site and staged actors from the guarded owned smoke colony. Does not dial gates, activate connections or transfer actors.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var source = Find.CurrentMap;
        var gate = source.listerThings.AllThings.OfType<Building_Gate>()
            .SingleOrDefault(g => g.Position == new IntVec3(110, 0, 130));
        if (gate == null || gate.IsActive || gate.IsOpeningQueued || gate.IsIrisActivated)
            throw new InvalidOperationException("Load the owned smoke save with its inactive, open-iris gate at (110,130).");
        var dhd = gate.ConnectedDHD;
        if (dhd == null || dhd.Position != new IntVec3(110, 0, 134))
            throw new InvalidOperationException("The owned source DHD at (110,134) must be linked before preparation.");
        if (Find.WorldObjects.MapParents.OfType<WorldObject_GateTransitSite>().Any(s => s.SiteName == DestinationName))
            throw new InvalidOperationException("Reload the owned smoke save before recreating this fixture.");
        var pawns = source.mapPawns.FreeColonistsSpawned.Where(p => !p.Dead && !p.Downed)
            .OrderBy(p => p.thingIDNumber).ToArray();
        if (pawns.Length < 2)
            throw new InvalidOperationException("Two healthy source colonists are required: traveler and source closer.");
        var tile = DestinationTile(source.Tile);

        ClearLanding(source, gate.Position);
        gate.SetFaction(Faction.OfPlayer);
        dhd.SetFaction(Faction.OfPlayer);
        EnsurePower(source, new IntVec3(105, 0, 130));
        for (int i = 0; i < pawns.Length; i++)
        {
            pawns[i].jobs.StopAll();
            pawns[i].drafter.Drafted = false;
            pawns[i].pather.StopDead();
            pawns[i].Position = new IntVec3(114 + i, 0, 134);
            var wait = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
            wait.expiryInterval = 60000;
            wait.playerForced = true;
            pawns[i].jobs.TryTakeOrderedJob(wait, JobTag.Misc);
        }
        var cargo = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Autopistol"));
        if (!pawns[0].inventory.innerContainer.TryAdd(cargo, false))
            throw new InvalidOperationException("Could not put the unique fixture pistol in the traveler's inventory.");
        var destination = (WorldObject_GateTransitSite)WorldObjectMaker.MakeWorldObject(RimgateDefOf.Rimgate_GateTransitSite);
        destination.Tile = tile;
        destination.SiteName = DestinationName;
        destination.InitState(true, true);
        Find.WorldObjects.Add(destination);
        return new
        {
            success = true, queued = false, sourceMapId = source.uniqueID,
            sourceGateId = gate.GetUniqueLoadID(), sourceDhdId = dhd.GetUniqueLoadID(),
            destinationParentId = destination.ID, destinationName = DestinationName,
            destinationTile = tile.tileId, destinationDesignation = GateUtil.GetGateDesignation(tile),
            pawnId = pawns[0].GetUniqueLoadID(), pawnName = pawns[0].LabelShort,
            closerId = pawns[1].GetUniqueLoadID(), closerName = pawns[1].LabelShort,
            itemId = cargo.GetUniqueLoadID(), itemDef = cargo.def.defName,
            limits = "Prepared a lazy destination via normal site address registration; not glyph/quest discovery. Real dialing must generate its map. Cargo starts in pawn inventory, not a separately loaded transporter."
        };
    });

    [Tool("rimgate/transit_status", Description = "Read gate links, queues, transporters and exact tracked pawn/item ownership across loaded maps and gate buffers. Re-resolves IDs after save/load; never activates or transfers.")]
    public static Task<object> Status(IRimBridgeContext context, string sourceGateId, int destinationParentId, string pawnId, string itemId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        var gates = Find.Maps.SelectMany(m => m.listerThings.AllThings.OfType<Building_Gate>()).ToArray();
        var source = gates.SingleOrDefault(g => Matches(g, sourceGateId));
        var parent = Find.WorldObjects.MapParents.SingleOrDefault(p => p.ID == destinationParentId);
        var destination = parent?.Map?.listerThings.AllThings.OfType<Building_Gate>().SingleOrDefault();
        var occurrences = new List<Occurrence>();
        var visited = new HashSet<IThingHolder>(ReferenceEqualityComparer.Instance);
        foreach (var map in Find.Maps)
        {
            WalkHolder(map, "map:" + map.uniqueID, map.uniqueID, visited, occurrences);
        }
        foreach (var gate in gates)
        {
            foreach (var pair in new[] { ("send", Buffer(gate, SendBuffer)), ("receive", Buffer(gate, ReceiveBuffer)) })
                foreach (var thing in pair.Item2)
                {
                    var where = gate.ThingID + ":" + pair.Item1;
                    occurrences.Add(new(thing, where, gate.Map.uniqueID, true));
                    if (thing is IThingHolder holder)
                        WalkHolder(holder, where, gate.Map.uniqueID, visited, occurrences);
                }
        }
        var actualWorldComp = Find.World.GetComponent<WorldComp_GateAddresses>();
        return new
        {
            success = true, ready = source != null && destination != null && source.ConnectedDHD != null && destination.ConnectedDHD != null,
            tick = Find.TickManager.TicksGame, currentMapId = Find.CurrentMap?.uniqueID,
            source = GateState(source), destination = GateState(destination),
            reciprocal = source != null && destination != null && source.ConnectedGate == destination && destination.ConnectedGate == source,
            currentWorldOwnsAddressCache = ReferenceEquals(GateUtil.WorldComp, actualWorldComp),
            worldComponentIsCurrent = ReferenceEquals(GateUtil.WorldComp, actualWorldComp),
            cachedAddresses = GateUtil.WorldComp.AddressList.Select(t => new { tile = t.tileId, designation = GateUtil.GetGateDesignation(t) }).ToArray(),
            worldPawnRegistrations = Find.WorldPawns.AllPawnsAliveOrDead.Where(p => Matches(p, pawnId)).Select(p => p.GetUniqueLoadID()).ToArray(),
            addresses = actualWorldComp.AddressList.Select(t => new { tile = t.tileId, designation = GateUtil.GetGateDesignation(t) }).ToArray(),
            pawn = Tracked(pawnId, occurrences), item = Tracked(itemId, occurrences),
            destinationParentId, destinationHasMap = parent?.HasMap == true,
            maps = Find.Maps.Select(m => new { mapId = m.uniqueID, mapIndex = m.Index, tile = m.Tile.tileId, parentId = m.Parent.ID, label = m.Parent.Label }).ToArray()
        };
    });

    [Tool("rimgate/stage_transit_approach", Description = "Stage the tracked traveler and a third colonist beside a mature outgoing gate on their current source map. Normal UI entry jobs must perform both transfers; this tool does not activate gates or change buffers.")]
    public static Task<object> StageApproach(IRimBridgeContext context, string expectedConfigPath,
        string expectedModRoot, string sourceGateId, string pawnId, string closerId, string checkpoint = "send")
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        if (checkpoint is not ("send" or "receive"))
            throw new ArgumentException("Checkpoint must be send or receive.", nameof(checkpoint));
        var source = Find.Maps.SelectMany(m => m.listerThings.AllThings.OfType<Building_Gate>())
            .Single(g => Matches(g, sourceGateId));
        if (Find.CurrentMap != source.Map || !source.IsActive || source.IsReceivingGate
            || source.IsOpeningQueued || source.TicksSinceOpened < 150
            || source.IsIrisActivated || source.ConnectedGate == null || !source.ConnectedGate.IsActive)
            throw new InvalidOperationException("View the mature active outgoing fixture gate before staging the approach.");
        var colonists = source.Map.mapPawns.FreeColonistsSpawned.Where(p => !p.Dead && !p.Downed).ToArray();
        var traveler = colonists.Single(p => Matches(p, pawnId));
        if (!colonists.Any(p => Matches(p, closerId)))
            throw new InvalidOperationException("The fixture closer must still be on the source map.");
        var dummy = colonists.Where(p => !Matches(p, pawnId) && !Matches(p, closerId))
            .OrderBy(p => p.thingIDNumber).FirstOrDefault()
            ?? throw new InvalidOperationException("A third healthy source colonist is required for the staggered arrival fixture.");
        var entry = source.InteractionCell;
        var approach = entry + new IntVec3(1, 0, 0);
        if (!entry.InBounds(source.Map) || !entry.Walkable(source.Map)
            || !approach.InBounds(source.Map) || !approach.Walkable(source.Map))
            throw new InvalidOperationException("Both same-map approach cells must be walkable.");
        var dummyCell = checkpoint == "send" ? approach : entry;
        var travelerCell = checkpoint == "send" ? entry : approach;
        StagePawn(dummy, dummyCell);
        StagePawn(traveler, travelerCell);
        return new
        {
            success = true, checkpoint, sourceMapId = source.Map.uniqueID, sourceGateId = source.GetUniqueLoadID(),
            dummyId = dummy.GetUniqueLoadID(), dummyName = dummy.LabelShort,
            pawnId = traveler.GetUniqueLoadID(), closerId,
            dummyCell = new { x = dummyCell.x, z = dummyCell.z },
            pawnCell = new { x = travelerCell.x, z = travelerCell.z },
            limits = "Same-map fixture positioning only. Send checkpoint: order tracked entry before saving. Receive checkpoint: order dummy entry then tracked entry through normal UI before stepping."
        };
    });

    [Tool("rimgate/view_transit_map", Description = "Switch the current viewed map to one loaded map in the isolated transit fixture. Does not move pawns or items.")]
    public static Task<object> ViewMap(IRimBridgeContext context, int mapId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        if (!Prefs.DevMode || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7")
            throw new InvalidOperationException("Use the isolated 1.7 developer profile.");
        var map = Find.Maps.Single(m => m.uniqueID == mapId);
        Current.Game.CurrentMap = map;
        return new { success = true, mapId, mapIndex = map.Index };
    });

    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var comparison = StringComparison.OrdinalIgnoreCase;
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), comparison)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), comparison))
            throw new InvalidOperationException("Refusing fixture changes outside the expected isolated 1.7 game/profile/deployment.");
    }

    private static PlanetTile DestinationTile(PlanetTile start)
    {
        var pending = new Queue<PlanetTile>();
        var visited = new HashSet<PlanetTile> { start };
        pending.Enqueue(start);
        var neighbors = new List<PlanetTile>();
        while (pending.Count > 0 && visited.Count < 500)
        {
            var tile = pending.Dequeue();
            if (tile != start && !Find.WorldObjects.AnyMapParentAt(tile)
                && !tile.Tile.PrimaryBiome.impassable && tile.Tile.hilliness != Hilliness.Impassable)
                return tile;
            neighbors.Clear();
            Find.WorldGrid.GetTileNeighbors(tile, neighbors);
            foreach (var next in neighbors)
                if (visited.Add(next)) pending.Enqueue(next);
        }
        throw new InvalidOperationException("No free reachable-surface neighboring tile for the destination fixture.");
    }

    private static void ClearLanding(Map map, IntVec3 center)
    {
        foreach (var cell in CellRect.CenteredOn(center, 12).ClipInsideMap(map))
        {
            map.roofGrid.SetRoof(cell, null);
            foreach (var thing in cell.GetThingList(map).ToArray())
                if (thing is not Pawn && thing is not Building_Gate && thing is not Building_DHD)
                    thing.Destroy(DestroyMode.Vanish);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        }
        map.fogGrid.ClearAllFog();
    }

    private static void EnsurePower(Map map, IntVec3 cell)
    {
        var generator = GenSpawn.Spawn(DefDatabase<ThingDef>.GetNamed("VanometricPowerCell"), cell, map, Rot4.North, WipeMode.Vanish);
        generator.SetFaction(Faction.OfPlayer);
    }

    private static void StagePawn(Pawn pawn, IntVec3 cell)
    {
        pawn.jobs.StopAll();
        pawn.pather.StopDead();
        pawn.Position = cell;
        var wait = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
        wait.expiryInterval = 60000;
        wait.playerForced = true;
        pawn.jobs.TryTakeOrderedJob(wait, JobTag.Misc);
    }

    private static IEnumerable<Thing> Buffer(Building_Gate gate, FieldInfo field)
        => (field.GetValue(gate) as IEnumerable)?.Cast<Thing>() ?? [];

    private static object GateState(Building_Gate gate) => gate == null ? null : new
    {
        id = gate.GetUniqueLoadID(), thingId = gate.ThingID, mapId = gate.Map.uniqueID, mapIndex = gate.Map.Index,
        tile = gate.GateAddress.tileId, designation = GateUtil.GetGateDesignation(gate.GateAddress),
        x = gate.Position.x, z = gate.Position.z, gate.IsActive, gate.IsReceivingGate,
        interactionCell = new { x = gate.InteractionCell.x, z = gate.InteractionCell.z,
            walkable = gate.InteractionCell.InBounds(gate.Map) && gate.InteractionCell.Walkable(gate.Map),
            standable = gate.InteractionCell.InBounds(gate.Map) && gate.InteractionCell.Standable(gate.Map),
            edifice = gate.InteractionCell.InBounds(gate.Map) ? gate.InteractionCell.GetEdifice(gate.Map)?.def.defName : null },
        openingQueued = gate.IsOpeningQueued, ticksUntilOpen = gate.TicksUntilOpen,
        connectedGateId = gate.ConnectedGate?.GetUniqueLoadID(), connectedTile = gate.ConnectedAddress.tileId,
        externalHolds = gate.ExternalHoldCount, gate.HasIris, irisClosed = gate.IsIrisActivated,
        powered = gate.Powered, ticksSinceOpened = gate.TicksSinceOpened,
        send = Buffer(gate, SendBuffer).Select(t => t.GetUniqueLoadID()).ToArray(),
        receive = Buffer(gate, ReceiveBuffer).Select(t => t.GetUniqueLoadID()).ToArray(),
        sendCount = Buffer(gate, SendBuffer).Count(), receiveCount = Buffer(gate, ReceiveBuffer).Count(),
        pendingOwned = (PendingOwner.GetValue(gate) as ThingOwner)?.Select(t => t.GetUniqueLoadID()).ToArray() ?? [],
        pendingOwnedCount = (PendingOwner.GetValue(gate) as ThingOwner)?.Count ?? 0,
        redraftIds = (RedraftIds.GetValue(gate) as IEnumerable<int> ?? []).OrderBy(id => id).ToArray(),
        playerPawnIds = gate.Map.mapPawns.FreeColonistsSpawned.Select(p => p.GetUniqueLoadID()).ToArray(),
        transporterCount = gate.Transporter?.innerContainer.Count ?? 0,
        transporter = gate.Transporter?.innerContainer.Select(t => t.GetUniqueLoadID()).ToArray(),
        loading = gate.Transporter?.LoadingInProgressOrReadyToLaunch == true,
        dhd = gate.ConnectedDHD == null ? null : new { id = gate.ConnectedDHD.GetUniqueLoadID(),
            x = gate.ConnectedDHD.Position.x, z = gate.ConnectedDHD.Position.z,
            powered = gate.ConnectedDHD.Powered, wantsClosed = gate.ConnectedDHD.WantsGateClosed,
            lastDialedTile = gate.ConnectedDHD.LastDialledAddress.tileId }
    };

    private sealed record Occurrence(Thing Thing, string Location, int MapId, bool BufferReference = false);

    private static void WalkHolder(IThingHolder holder, string location, int mapId,
        HashSet<IThingHolder> visited, List<Occurrence> occurrences)
    {
        if (!visited.Add(holder)) return;
        var contents = holder.GetDirectlyHeldThings();
        if (contents != null)
            foreach (var thing in contents)
                occurrences.Add(new(thing, location + "/" + holder.GetType().Name, mapId));
        var children = new List<IThingHolder>();
        holder.GetChildHolders(children);
        foreach (var child in children)
            WalkHolder(child, location + "/" + holder.GetType().Name, mapId, visited, occurrences);
    }

    private static bool Matches(Thing thing, string id) => thing.ThingID == id || "Thing_" + thing.ThingID == id;

    private static object Tracked(string id, List<Occurrence> occurrences)
    {
        var hits = occurrences.Where(o => Matches(o.Thing, id)).ToArray();
        var pawn = hits.FirstOrDefault()?.Thing as Pawn;
        return new
        {
            id, occurrenceCount = hits.Length,
            distinctObjectCount = hits.Select(o => o.Thing).Distinct(ReferenceEqualityComparer.Instance).Count(),
            ownershipOccurrenceCount = hits.Count(o => !o.BufferReference),
            referenceBufferCount = hits.Count(o => o.BufferReference),
            locations = hits.Select(o => new { o.Location, mapId = o.MapId, thingId = o.Thing.GetUniqueLoadID(),
                defName = o.Thing.def.defName, spawned = o.Thing.Spawned, destroyed = o.Thing.Destroyed,
                stackCount = o.Thing.stackCount, bufferReference = o.BufferReference,
                parentHolder = o.Thing.ParentHolder?.GetType().Name }).ToArray(),
            pawn = pawn == null ? null : new { pawn.Dead, pawn.Downed, pawn.Drafted, pawn.Spawned,
                job = pawn.CurJob?.def.defName, mapId = pawn.Map?.uniqueID,
                inventory = pawn.inventory?.innerContainer.Select(t => t.GetUniqueLoadID()).ToArray() }
        };
    }
}
