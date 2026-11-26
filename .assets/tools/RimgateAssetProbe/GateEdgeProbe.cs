using System.Reflection;
using System.Text.Json;
using RimBridgeServer.Sdk;
using RimWorld;
using Verse;

namespace Rimgate.AssetProbe;

// Ordinary loose Steel congestion, plus native read-only placement diagnostics.
// Admission, retries, closure and world-map lifetime remain production-owned.
public sealed class GateEdgeProbe
{
    private const float BlockRadius = 16f;
    private const string DestinationName = "Rimgate transit destination";
    private static readonly MethodInfo NearSearch = typeof(GenPlace).GetMethod("TryFindPlaceSpotNear", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo SpotQuality = typeof(GenPlace).GetMethod("PlaceSpotQualityAt", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly FieldInfo SearchCount = typeof(GenPlace).GetField("PlaceNearMaxRadialCells", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo BestDrop = typeof(Building_Gate).Assembly.GetType("Rimgate.Utils", true)!
        .GetMethod("BestDropCellNearThing", BindingFlags.Static | BindingFlags.Public)!;
    // One observational reference only: it never admits cargo or supplies a save owner.
    private static Game _watchGame;
    private static Thing _watchedCargo;
    private static readonly FieldInfo StoredThings = typeof(WorldObject_GateTransitSite).GetField("_storedThings", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Tool("rimgate/prepare_blocked_landing", Description = "Congest the guarded receiving gate's landing search with tracked loose Steel1 inputs, preserving existing things. Requires actual source Steel cargo and verifies the exact native Near search cannot place it; does not admit or drop cargo.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string sourceGateId, string receiverGateId, int destinationParentId, string cargoId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var (source, destination, receiver) = Resolve(sourceGateId, receiverGateId, destinationParentId);
        if (receiver == null || !source.IsActive || source.IsReceivingGate || !receiver.IsActive || !receiver.IsReceivingGate
            || source.IsOpeningQueued || receiver.IsOpeningQueued || source.TicksSinceOpened < 150 || receiver.TicksSinceOpened < 150
            || source.ConnectedGate != receiver || receiver.ConnectedGate != source
            || source.IsIrisActivated || receiver.IsIrisActivated
            || source.GetDirectlyHeldThings().Count != 0 || receiver.GetDirectlyHeldThings().Count != 0)
            throw new InvalidOperationException("Use the mature reciprocal open-iris link, with no pending cargo.");
        var cargo = FindCargo(cargoId) ?? throw new InvalidOperationException("The actual source cargo must exist.");
        if (cargo.def != ThingDefOf.Steel || cargo.stackCount != 1 || !cargo.Spawned || cargo.Map != source.Map)
            throw new InvalidOperationException("Stage one ordinary loose source Steel1 through normal fixture inputs first.");
        var map = receiver.Map;
        var center = DropCenter(receiver);
        int count = NativeSearchCount();
        if (GenRadial.RadialPattern.Take(count).Any(offset =>
            (center + offset).DistanceToSquared(receiver.Position) > BlockRadius * BlockRadius))
            throw new InvalidOperationException("The bounded Steel apron does not cover this gate's complete native search.");
        var initialThings = map.listerThings.AllThings.Select(t => new Protected(t, t.Spawned, t.Destroyed,
            t.Map, t.Position, t.HitPoints, t.stackCount)).ToArray();
        var initialCargoOwner = cargo.ParentHolder;
        var initialCargoPosition = cargo.Position;
        var initiallyEmpty = new HashSet<IntVec3>(GenRadial.RadialPattern.Take(count).Select(offset => center + offset)
            .Where(cell => cell.InBounds(map) && cell.Standable(map) && cell.GetThingList(map).Count == 0));
        var created = new List<Thing>();
        int alreadyCompatible = 0, cannotSpawn = 0, atItemCapacity = 0;
        try
        {
            foreach (var cell in GenRadial.RadialCellsAround(receiver.Position, BlockRadius, true))
            {
                if (!cell.InBounds(map)) continue;
                var existing = cell.GetThingList(map);
                if (existing.Any(other => cargo.CanStackWith(other))) { alreadyCompatible++; continue; }
                // Native Near quality already rejects a full cell with no compatible stack.
                // Spawning another item here would relocate (or destroy) original items.
                if (cell.GetItemCount(map) >= cell.GetMaxItemsAllowedInCell(map)) { atItemCapacity++; continue; }
                if (!GenSpawn.CanSpawnAt(ThingDefOf.Steel, cell, map, Rot4.North, true)) { cannotSpawn++; continue; }
                if (existing.Any(other => GenSpawn.SpawningWipes(ThingDefOf.Steel, other.def, true)))
                    throw new InvalidOperationException("A loose Steel input would wipe a preexisting object; no fixture may do that.");
                var blocker = ThingMaker.MakeThing(ThingDefOf.Steel);
                blocker.stackCount = 1;
                GenSpawn.Spawn(blocker, cell, map, Rot4.North, WipeMode.Vanish);
                blocker.SetForbidden(true, false);
                created.Add(blocker);
            }
            if (DropCenter(receiver) != center)
                throw new InvalidOperationException("Loose inputs unexpectedly changed the production search origin.");
            VerifyProtected(initialThings, map);
            if (!cargo.Spawned || cargo.Map != source.Map || cargo.Position != initialCargoPosition
                || !ReferenceEquals(cargo.ParentHolder, initialCargoOwner) || cargo.stackCount != 1)
                throw new InvalidOperationException("The original source cargo must remain untouched.");
            var placement = Inspect(receiver, cargo);
            if (placement.Found || placement.Cells.Any(c => c.Quality > 0))
                throw new InvalidOperationException("Native placement still has a hole; this fixture is not a blocked-placement case.");
            var release = created.FirstOrDefault(t => initiallyEmpty.Contains(t.Position))
                ?? throw new InvalidOperationException("A tracked input on an initially empty safe landing is required for one-cell recovery.");
            return new
            {
                success = true, sourceGateId, receiverGateId, destinationParentId, cargoId,
                cargoDef = cargo.def.defName, cargoCount = cargo.stackCount, mapId = map.uniqueID,
                radius = BlockRadius, createdCount = created.Count, alreadyCompatible, cannotSpawn, atItemCapacity,
                blockerIds = string.Join(",", created.Select(t => t.GetUniqueLoadID())),
                releaseBlockerId = release.GetUniqueLoadID(), releaseCell = Cell(release.Position),
                search = PlacementReport(placement, false), anyPawnBlockingMapRemoval = map.mapPawns.AnyPawnBlockingMapRemoval,
                protectedThingCount = initialThings.Length,
                limits = "Ordinary loose Steel1 inputs exercise the production no-merge identity policy. Occupied cells were not skipped; only native CanSpawnAt-false cells, already-full item cells, or already-compatible piles needed no new item. No gate outcome, pawn, terrain, power or controller was changed. Actual arrival failure, deep ownership, closure and recovery must be observed through real jobs/ticks. No player pawn is added to prevent unloading."
            };
        }
        catch
        {
            foreach (var thing in created)
                if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
            throw;
        }
    });

    [Tool("rimgate/blocked_landing_report", Description = "Read the complete native Near-placement search and gate-owned cargo, or the transit site's retained value records after its map unloads. Does not place, admit, transfer, close or recreate anything.")]
    public static Task<object> Report(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string sourceGateId, string receiverGateId, int destinationParentId, string cargoId, bool includeCells = false)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var (source, destination, receiver) = Resolve(sourceGateId, receiverGateId, destinationParentId);
        var cargo = FindCargo(cargoId);
        var records = ((List<TransitStoredThing>)StoredThings.GetValue(destination)!) ?? [];
        var stored = new List<object>();
        Flatten(records, "site", stored);
        var corpseOwner = ((IThingHolder)destination).GetDirectlyHeldThings();
        var siteChildren = new List<IThingHolder>();
        destination.GetChildHolders(siteChildren);
        var storedCorpses = corpseOwner?.OfType<Corpse>().Select(c => new
        {
            id = c.GetUniqueLoadID(), spawned = c.Spawned, destroyed = c.Destroyed,
            parentHolder = c.ParentHolder?.GetType().FullName, directOwnerIsSite = ReferenceEquals(corpseOwner.Owner, destination),
            childTraversalIncludesCorpse = siteChildren.Any(h => ReferenceEquals(h, c)),
            innerPawnId = c.InnerPawn?.GetUniqueLoadID(), innerPawnDead = c.InnerPawn?.Dead,
            innerPawnOwnedByCorpse = c.InnerPawn != null && ReferenceEquals(c.InnerPawn.ParentHolder, c),
            worldPawnRegistered = c.InnerPawn != null && Find.WorldPawns.AllPawnsAliveOrDead.Any(p => ReferenceEquals(p, c.InnerPawn)),
            inventory = c.InnerPawn?.inventory?.innerContainer.Select(t => new { id = t.GetUniqueLoadID(), defName = t.def.defName,
                count = t.stackCount, ownerIsInnerPawnInventory = ReferenceEquals(t.ParentHolder, c.InnerPawn.inventory) }).ToArray(),
            equipmentIds = c.InnerPawn?.equipment?.AllEquipmentListForReading.Select(t => t.GetUniqueLoadID()).ToArray(),
            apparelIds = c.InnerPawn?.apparel?.WornApparel.Select(t => t.GetUniqueLoadID()).ToArray()
        }).ToArray();
        return new
        {
            success = true, tick = Find.TickManager.TicksGame, sourceGateId, receiverGateId, destinationParentId, cargoId,
            sourceActive = source.IsActive, sourcePending = source.GetDirectlyHeldThings().Select(t => t.GetUniqueLoadID()).ToArray(),
            destinationHasMap = destination.HasMap, mapId = destination.Map?.uniqueID,
            siteScavenged = (bool)typeof(WorldObject_GateTransitSite).GetField("_wasLooted", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(destination)!,
            receiverFoundByOriginalId = receiver != null, receiverActive = receiver?.IsActive,
            receiverPending = receiver?.GetDirectlyHeldThings().Select(t => t.GetUniqueLoadID()).ToArray(),
            destinationGateIds = destination.Map?.listerThings.AllThings.OfType<Building_Gate>().Select(g => g.GetUniqueLoadID()).ToArray(),
            destinationGates = destination.Map?.listerThings.AllThings.OfType<Building_Gate>()
                .Select(g => new { id = g.GetUniqueLoadID(), cell = Cell(g.Position), rotation = g.Rotation.AsInt,
                    vortexCells = g.VortexCells.Select(Cell).ToArray() }).ToArray(),
            anyPawnBlockingMapRemoval = destination.Map?.mapPawns.AnyPawnBlockingMapRemoval,
            cargo = cargo == null ? null : new
            {
                id = cargo.GetUniqueLoadID(), defName = cargo.def.defName, count = cargo.stackCount,
                spawned = cargo.Spawned, destroyed = cargo.Destroyed, mapId = cargo.Map?.uniqueID,
                parentHolder = cargo.ParentHolder?.GetType().FullName,
                sourceOwnsReference = source.GetDirectlyHeldThings().Any(t => ReferenceEquals(t, cargo)),
                receiverOwnsReference = receiver?.GetDirectlyHeldThings().Any(t => ReferenceEquals(t, cargo)) == true
            },
            placement = receiver != null && cargo != null ? PlacementReport(Inspect(receiver, cargo), includeCells) : null,
            storedRecords = stored.ToArray(),
            storedCorpseCount = storedCorpses?.Length ?? 0, storedCorpses,
            loadedItems = destination.Map?.listerThings.AllThings.Where(t => t.def.category == ThingCategory.Item).Select(t => new { id = t.GetUniqueLoadID(), defName = t.def.defName, count = t.stackCount, faction = t.Faction?.GetUniqueLoadID(), cell = Cell(t.Position), innerPawnId = (t as Corpse)?.InnerPawn?.GetUniqueLoadID() }).ToArray(),
            limits = "Ordinary TransitStoredThing value records retain definition/count/nested contents, not original IDs. Corpses use the site's separate deep owner and must retain their exact pawn/inventory identities; world-pawn registration is observed without changes. Native placement inspection invokes deterministic read-only search/quality methods, never TryDrop or admission. Steel-specific congestion does not prove every cargo type is blocked."
        };
    });

    [Tool("rimgate/release_blocked_landing", Description = "Remove exactly one listed fixture Steel1 input to open one landing after reload or closure. Preserves original cargo and all other piles; normal gate ticks must perform recovery.")]
    public static Task<object> Release(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string sourceGateId, string receiverGateId, int destinationParentId, string cargoId,
        string blockerIds, string releaseBlockerId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var (_, _, receiver) = Resolve(sourceGateId, receiverGateId, destinationParentId);
        if (receiver == null) throw new InvalidOperationException("The original receiver map must still exist; inspect stored records if it unloaded.");
        var ids = blockerIds.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        if (!ids.Contains(releaseBlockerId) || releaseBlockerId == cargoId)
            throw new InvalidOperationException("Release only the tracked blocker chosen by preparation, never the original cargo.");
        var cargo = FindCargo(cargoId) ?? throw new InvalidOperationException("The exact cargo must still exist before recovery.");
        var blocker = receiver.Map.listerThings.AllThings.SingleOrDefault(t => t.GetUniqueLoadID() == releaseBlockerId);
        if (blocker == null || blocker.def != ThingDefOf.Steel || blocker.stackCount != 1
            || !blocker.Spawned || blocker.Map != receiver.Map
            || blocker.Position.DistanceToSquared(receiver.Position) > BlockRadius * BlockRadius
            || blocker.Position.GetEdifice(receiver.Map) != null || !blocker.Position.Standable(receiver.Map))
            throw new InvalidOperationException("The chosen fixture Steel must remain on its ordinary safe landing.");
        var before = Inspect(receiver, cargo);
        if (before.Found) throw new InvalidOperationException("The receiver is no longer fully blocked; inspect before altering inputs.");
        var cell = blocker.Position;
        blocker.Destroy(DestroyMode.Vanish);
        var after = Inspect(receiver, cargo);
        return new
        {
            success = after.Found, removedId = releaseBlockerId, releasedCell = Cell(cell),
            receiverActive = receiver.IsActive, search = PlacementReport(after, false),
            limits = "Only one caller-listed Steel input was removed. This is input release, not cargo placement; actual gate retry and queue cleanup remain to be observed."
        };
    });

    [Tool("rimgate/prepare_receiver_power", Description = "Place one ordinary player vanometric cell on a native-valid empty 1x2 footprint within four cells of the owned receiver. Preserves original objects; native ticks must connect power. Does not change iris state or move operators.")]
    public static Task<object> PrepareReceiverPower(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string sourceGateId, string receiverGateId, int destinationParentId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        ResetWatchForCurrentGame();
        Guard(expectedConfigPath, expectedModRoot);
        var (_, _, receiver) = Resolve(sourceGateId, receiverGateId, destinationParentId);
        if (receiver == null) throw new InvalidOperationException("The original receiving map and gate must be loaded.");
        var map = receiver.Map;
        var definition = DefDatabase<ThingDef>.GetNamed("VanometricPowerCell");
        var cell = GenRadial.RadialCellsAround(receiver.Position, 4f, true).FirstOrDefault(candidate =>
            GenAdj.OccupiedRect(candidate, Rot4.North, definition.Size).All(c =>
                c.InBounds(map) && c.DistanceToSquared(receiver.Position) <= 16 && c.GetThingList(map).Count == 0)
            && GenSpawn.CanSpawnAt(definition, candidate, map, Rot4.North, false), IntVec3.Invalid);
        if (!cell.IsValid) throw new InvalidOperationException("No safe empty native-valid receiver power footprint exists within four cells.");
        var before = map.listerThings.AllThings.Select(t => new Protected(t, t.Spawned, t.Destroyed,
            t.Map, t.Position, t.HitPoints, t.stackCount)).ToArray();
        var generator = ThingMaker.MakeThing(definition);
        generator.SetFaction(Faction.OfPlayer);
        try
        {
            GenSpawn.Spawn(generator, cell, map, Rot4.North, WipeMode.Vanish);
            VerifyProtected(before, map);
        }
        catch
        {
            if (!generator.Destroyed) generator.Destroy(DestroyMode.Vanish);
            throw;
        }
        return new
        {
            success = generator.Spawned, generatorId = generator.GetUniqueLoadID(), cell = Cell(cell),
            footprint = generator.OccupiedRect().Select(Cell).ToArray(), receiverGateId, mapId = map.uniqueID,
            receiverPowered = receiver.Powered, receiver.HasIris, irisClosed = receiver.IsIrisActivated,
            limits = "One vanilla generator is an input. Native power connection/evaluation, actual iris installation input and real operator toggle jobs remain separate. No PowerOn, iris state or operator position was set."
        };
    });

    [Tool("rimgate/receiver_iris_report", Description = "Read owned receiver iris/power/controller and optional operator. Optionally watch one actual cargo reference before real iris destruction; the watch is not ownership, is cleared when the game changes, and clearWatch releases it explicitly.")]
    public static Task<object> ReceiverIrisReport(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string sourceGateId, string receiverGateId, int destinationParentId, string operatorId = "",
        string watchCargoId = "", bool clearWatch = false)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        bool gameChanged = ResetWatchForCurrentGame();
        if (clearWatch) { _watchedCargo = null; _watchGame = null; }
        Guard(expectedConfigPath, expectedModRoot);
        var (source, destination, receiver) = Resolve(sourceGateId, receiverGateId, destinationParentId);
        if (!clearWatch && !string.IsNullOrEmpty(watchCargoId))
        {
            if (_watchedCargo != null && _watchedCargo.GetUniqueLoadID() != watchCargoId)
                throw new InvalidOperationException("Clear the previous single-cargo watch before watching another object.");
            if (_watchedCargo == null)
            {
                var input = FindCargo(watchCargoId)
                    ?? throw new InvalidOperationException("Start the watch while that exact cargo still exists in a loaded owner/map.");
                if (input.def.category != ThingCategory.Item)
                    throw new InvalidOperationException("This observational watch is limited to one actual cargo item.");
                _watchedCargo = input;
                _watchGame = Current.Game;
            }
        }
        var cargo = _watchedCargo;
        var loadedCargo = cargo == null ? null : FindCargo(cargo.GetUniqueLoadID());
        var actor = string.IsNullOrEmpty(operatorId) ? null : FindCargo(operatorId) as Pawn;
        var dhd = receiver?.ConnectedDHD;
        return new
        {
            success = true, tick = Find.TickManager.TicksGame, destinationHasMap = destination.HasMap,
            receiverFound = receiver != null, receiverGateId, receiverActive = receiver?.IsActive,
            hasIris = receiver?.HasIris, irisClosed = receiver?.IsIrisActivated, powered = receiver?.Powered,
            powerOutputWatts = receiver?.PowerTrader?.PowerOutput, connected = receiver?.PowerTrader?.PowerNet != null,
            dhdId = dhd?.GetUniqueLoadID(), dhdPowered = dhd?.Powered,
            operatorState = actor == null ? null : new
            {
                id = actor.GetUniqueLoadID(), spawned = actor.Spawned, mapId = actor.Map?.uniqueID,
                cell = Cell(actor.Position), dead = actor.Dead, downed = actor.Downed,
                job = actor.CurJobDef?.defName, jobTarget = actor.CurJob?.targetA.Thing?.GetUniqueLoadID()
            },
            watchClearedForGameChange = gameChanged, watchActive = cargo != null, watchExplicitlyCleared = clearWatch,
            watchedCargo = cargo == null ? null : new
            {
                id = cargo.GetUniqueLoadID(), defName = cargo.def.defName, count = cargo.stackCount,
                destroyed = cargo.Destroyed, spawned = cargo.Spawned, parentHolder = cargo.ParentHolder?.GetType().FullName,
                loadedLookupFound = loadedCargo != null, loadedLookupSameReference = ReferenceEquals(loadedCargo, cargo),
                sourceOwnsReference = source.GetDirectlyHeldThings().Any(t => ReferenceEquals(t, cargo)),
                receiverOwnsReference = receiver?.GetDirectlyHeldThings().Any(t => ReferenceEquals(t, cargo)) == true
            },
            limits = "One strong observational reference proves the same object's Destroyed flag after actual gameplay; it does not create ownership or prove an unseen cause by itself. It cannot establish object-reference identity across loading a new Game. Call clearWatch=true after the case; no jobs, iris/power values, positions or ticks are changed."
        };
    });

    private static bool ResetWatchForCurrentGame()
    {
        if (_watchGame == null || ReferenceEquals(_watchGame, Current.Game)) return false;
        _watchedCargo = null;
        _watchGame = null;
        return true;
    }

    private sealed record Protected(Thing Thing, bool Spawned, bool Destroyed, Map Map, IntVec3 Position, int HitPoints, int Count);
    private sealed record Candidate(IntVec3 Cell, bool CanSpawn, bool CompatibleBlocked, int Quality);
    private sealed record Placement(IntVec3 Center, bool Found, IntVec3 Best, List<Candidate> Cells);
    private static Placement Inspect(Building_Gate gate, Thing cargo)
    {
        var map = gate.Map;
        var center = DropCenter(gate);
        Predicate<IntVec3> validator = cell => !cell.GetThingList(map).Any(other => cargo.CanStackWith(other));
        object[] args = [center, cargo.def.defaultPlacingRot, map, cargo, true, IntVec3.Invalid, validator];
        bool found = (bool)NearSearch.Invoke(null, args)!;
        var cells = new List<Candidate>();
        foreach (var offset in GenRadial.RadialPattern.Take(NativeSearchCount()))
        {
            var cell = center + offset;
            bool canSpawn = cell.InBounds(map) && GenSpawn.CanSpawnAt(cargo.def, cell, map, cargo.def.defaultPlacingRot, true);
            bool blocked = cell.InBounds(map) && !validator(cell);
            int quality = Convert.ToInt32(SpotQuality.Invoke(null, [cell, cargo.def.defaultPlacingRot, map, cargo, center, true, validator]));
            cells.Add(new(cell, canSpawn, blocked, quality));
        }
        return new(center, found, (IntVec3)args[5], cells);
    }
    private static object PlacementReport(Placement placement, bool includeCells) => new
    {
        center = Cell(placement.Center), nativeCanPlace = placement.Found, bestSpot = Cell(placement.Best),
        candidateCount = placement.Cells.Count, nativeCanSpawnCount = placement.Cells.Count(c => c.CanSpawn),
        compatibleStackVetoCount = placement.Cells.Count(c => c.CompatibleBlocked),
        positiveQualityCount = placement.Cells.Count(c => c.Quality > 0),
        cells = includeCells ? placement.Cells.Select(c => new { x = c.Cell.x, z = c.Cell.z,
            nativeCanSpawn = c.CanSpawn, compatibleStackVeto = c.CompatibleBlocked, nativeQuality = c.Quality }).ToArray() : null
    };
    private static IntVec3 DropCenter(Building_Gate gate) => (IntVec3)BestDrop.Invoke(null, [gate])!;
    private static int NativeSearchCount() => (int)SearchCount.GetValue(null)!;
    private static object Cell(IntVec3 cell) => new { x = cell.x, z = cell.z };
    private static void VerifyProtected(Protected[] before, Map map)
    {
        var changed = before.Where(p => p.Thing.Destroyed != p.Destroyed || p.Thing.Spawned != p.Spawned
            || !ReferenceEquals(p.Thing.Map, p.Map) || p.Thing.Position != p.Position
            || p.Thing.HitPoints != p.HitPoints || p.Thing.stackCount != p.Count).ToArray();
        if (changed.Length != 0)
            throw new InvalidOperationException("Destination preservation failed: " + JsonSerializer.Serialize(new
            {
                mapId = map.uniqueID, changedCount = changed.Length,
                initialUnspawnedCount = before.Count(p => !p.Spawned),
                changes = changed.Select(p => new
                {
                    id = p.Thing.GetUniqueLoadID(), defName = p.Thing.def.defName,
                    spawnedBefore = p.Spawned, spawnedAfter = p.Thing.Spawned,
                    destroyedBefore = p.Destroyed, destroyedAfter = p.Thing.Destroyed,
                    mapBefore = p.Map?.uniqueID, mapAfter = p.Thing.Map?.uniqueID,
                    positionBefore = p.Position.ToString(), positionAfter = p.Thing.Position.ToString(),
                    hpBefore = p.HitPoints, hpAfter = p.Thing.HitPoints,
                    countBefore = p.Count, countAfter = p.Thing.stackCount
                }).ToArray()
            }));
    }
    private static void Flatten(IEnumerable<TransitStoredThing> records, string path, List<object> result)
    {
        int index = 0;
        foreach (var record in records)
        {
            string here = path + "/" + index++;
            result.Add(new { path = here, defName = record.Def?.defName, count = record.Count,
                stuff = record.Stuff?.defName, childCount = record.Contents?.Count ?? 0,
                hasOriginalThingId = false, position = record.Pos.HasValue ? Cell(record.Pos.Value) : null });
            if (record.Contents != null) Flatten(record.Contents, here, result);
        }
    }
    private static Thing FindCargo(string id)
    {
        var things = new List<Thing>();
        var visited = new HashSet<IThingHolder>(ReferenceEqualityComparer.Instance);
        void Walk(IThingHolder holder)
        {
            if (!visited.Add(holder)) return;
            var owner = holder.GetDirectlyHeldThings();
            if (owner != null) things.AddRange(owner);
            var children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            foreach (var child in children) Walk(child);
        }
        foreach (var map in Find.Maps) Walk(map);
        foreach (var site in Find.WorldObjects.MapParents.OfType<WorldObject_GateTransitSite>()) Walk(site);
        return things.Distinct(ReferenceEqualityComparer.Instance).Cast<Thing>().SingleOrDefault(t => t.GetUniqueLoadID() == id);
    }
    private static (Building_Gate Source, WorldObject_GateTransitSite Destination, Building_Gate Receiver) Resolve(
        string sourceGateId, string receiverGateId, int destinationParentId)
    {
        var source = Find.Maps.SelectMany(m => m.listerThings.AllThings.OfType<Building_Gate>())
            .SingleOrDefault(g => g.GetUniqueLoadID() == sourceGateId);
        var destination = Find.WorldObjects.MapParents.OfType<WorldObject_GateTransitSite>()
            .SingleOrDefault(p => p.ID == destinationParentId && p.SiteName == DestinationName);
        if (source == null || source.Position != new IntVec3(110, 0, 130) || destination == null || destination.Map == source.Map)
            throw new InvalidOperationException("Use the exact source gate and owned transit destination IDs.");
        var receiver = destination.Map?.listerThings.AllThings.OfType<Building_Gate>()
            .SingleOrDefault(g => g.GetUniqueLoadID() == receiverGateId);
        return (source, destination, receiver);
    }
    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        ResetWatchForCurrentGame();
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected isolated 1.7 profile and mod deployment.");
    }
}
