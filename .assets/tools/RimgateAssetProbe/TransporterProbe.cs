using RimBridgeServer.Sdk;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.AssetProbe;

// Native manifest/loading inputs and observations; no direct owner or manifest writes.
public sealed class TransporterProbe
{
    [Tool("rimgate/prepare_transporter_pawn", Description = "Create one healthy player fixture colonist with a unique inventory marker, and optionally one loose rifle/Home cell, beside the mature owned transit source. Does not assign a manifest, lord, boarding/hauling job, or owner transition.")]
    public static Task<object> PreparePawn(IRimBridgeContext context, string expectedConfigPath,
        string expectedModRoot, string sourceGateId, int destinationParentId, string closerId, bool mixed = false)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        var gate = map.listerThings.AllThings.OfType<Building_Gate>().Single(g => Id(g) == sourceGateId);
        var destination = Find.WorldObjects.MapParents.OfType<WorldObject_GateTransitSite>()
            .Single(s => s.ID == destinationParentId && s.SiteName == "Rimgate transit destination");
        if (gate.Position != new IntVec3(110, 0, 130) || !gate.IsActive || gate.IsReceivingGate
            || gate.IsOpeningQueued || gate.TicksSinceOpened < 150 || gate.IsIrisActivated
            || gate.ConnectedGate == null || gate.ConnectedGate.IsIrisActivated || gate.ConnectedGate.Map != destination.Map
            || gate.GetDirectlyHeldThings().Count != 0 || gate.ConnectedGate.GetDirectlyHeldThings().Count != 0
            || gate.Transporter == null || gate.Transporter.LoadingInProgressOrReadyToLaunch || gate.Transporter.innerContainer.Count != 0
            || !map.mapPawns.FreeColonistsSpawned.Any(p => Id(p) == closerId && !p.Dead && !p.Downed))
            throw new InvalidOperationException("Use the mature owned reciprocal empty open-iris source and retain its separate source closer.");
        var cells = new CellRect(133, 118, 7, 7).Cells.Where(c => c.InBounds(map) && c.Walkable(map)
            && c.GetThingList(map).Count == 0).ToArray();
        if (cells.Length < (mixed ? 2 : 1)) throw new InvalidOperationException("Two untouched empty walkable staging cells are required.");
        // The owned smoke input contains sleeping ancient guards; remove only
        // these hostile non-colony inputs before native loading wakes the map.
        var hostiles = map.listerThings.AllThings.OfType<Pawn>().Where(p => p.Spawned && !p.Dead
            && p.Faction != Faction.OfPlayer && !p.IsColonist && !p.IsPrisonerOfColony && !p.IsSlaveOfColony
            && (p.Faction?.HostileTo(Faction.OfPlayer) == true || p.HostileTo(Faction.OfPlayer))).ToArray();
        var removedHostileIds = hostiles.Select(Id).ToArray();
        foreach (var hostile in hostiles) hostile.Destroy(DestroyMode.Vanish);
        var boarder = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false,
            fixedBiologicalAge: 25f, fixedChronologicalAge: 25f));
        boarder.Name = new NameTriple("Rimgate", "Boarder", mixed ? "Mixed" : "Solo");
        var marker = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Autopistol"));
        if (boarder.inventory?.innerContainer.TryAdd(marker, false) != true)
            throw new InvalidOperationException("The fresh boarder must own its unique inventory marker.");
        GenSpawn.Spawn(boarder, cells[0], map);
        Thing item = null;
        if (mixed)
        {
            item = GenSpawn.Spawn(DefDatabase<ThingDef>.GetNamed("Gun_BoltActionRifle"), cells[1], map);
            item.SetForbidden(false, false);
            map.areaManager.Home[item.Position] = true;
        }
        var transporters = new List<CompTransporter> { gate.Transporter };
        return new { success = true, sourceGateId, destinationParentId, closerId, mixed, sourceMapId = map.uniqueID,
            boarderId = Id(boarder), boarderLabel = ColoredText.StripTags(boarder.LabelShort), markerId = Id(marker), itemId = item == null ? "" : Id(item), removedHostileIds,
            boarderCell = new { x = boarder.Position.x, z = boarder.Position.z },
            boarderNativeSendable = TransporterUtility.AllSendablePawns(transporters, map).Any(p => ReferenceEquals(p, boarder)),
            itemNativeSendable = item == null || TransporterUtility.AllSendableItems(transporters, map).Any(t => ReferenceEquals(t, item)),
            limits = "One fresh healthy colonist/inventory marker, optional loose rifle and its Home cell, and removal of hostile non-colony smoke guards only. Colony actors and gates stay intact. Use the native People checkbox and optional Items quantity, then actual native EnterTransporter/HaulToTransporter jobs." };
    });

    [Tool("rimgate/transporter_pawn_status", Description = "Read exact boarder/marker ownership, native boarding job, manifest and current native gate commands. Reacquires all IDs after reload; never initiates boarding, cancels or acknowledges loading.")]
    public static async Task<object> PawnStatus(IRimBridgeContext context, string expectedConfigPath,
        string expectedModRoot, string sourceGateId, int destinationParentId, string boarderId, string markerId, string itemId = "")
    {
        var transit = await TransitProbe.Status(context, sourceGateId, destinationParentId, boarderId, markerId);
        var cargo = string.IsNullOrEmpty(itemId) ? null : await TransitProbe.Status(context, sourceGateId, destinationParentId, boarderId, itemId);
        return await context.MainThread.InvokeAsync<object>(() =>
        {
            Guard(expectedConfigPath, expectedModRoot);
            var gate = Find.Maps.SelectMany(m => m.listerThings.AllThings.OfType<Building_Gate>()).Single(g => Id(g) == sourceGateId);
            var destination = Find.WorldObjects.MapParents.Single(s => s.ID == destinationParentId);
            var all = new List<Thing>();
            var visited = new HashSet<IThingHolder>(ReferenceEqualityComparer.Instance);
            void Walk(IThingHolder holder)
            {
                if (!visited.Add(holder)) return;
                if (holder.GetDirectlyHeldThings() is { } owner) all.AddRange(owner);
                var children = new List<IThingHolder>();
                holder.GetChildHolders(children);
                foreach (var child in children) Walk(child);
            }
            foreach (var map in Find.Maps) Walk(map);
            var boarder = all.OfType<Pawn>().FirstOrDefault(p => Id(p) == boarderId);
            var marker = all.FirstOrDefault(t => Id(t) == markerId);
            var transporter = gate.Transporter;
            var commands = gate.GetGizmos().OfType<Command>().Select(c => new { label = ColoredText.StripTags(c.Label), disabled = c.Disabled }).ToArray();
            return new { success = true, tick = Find.TickManager.TicksGame, gateActive = gate.IsActive,
                loading = transporter.LoadingInProgressOrReadyToLaunch, groupId = transporter.groupID,
                remaining = transporter.leftToLoad?.Sum(t => t.CountToTransfer) ?? 0,
                boarderManifestCount = transporter.leftToLoad?.Where(t => t.things.Any(p => p != null && Id(p) == boarderId)).Sum(t => t.CountToTransfer) ?? 0,
                itemManifestCount = transporter.leftToLoad?.Where(t => t.things.Any(p => p != null && Id(p) == itemId)).Sum(t => t.CountToTransfer) ?? 0,
                transporterCount = transporter.innerContainer.Count, transporterIds = transporter.innerContainer.Select(Id).ToArray(),
                transporterHasBoarder = transporter.innerContainer.Any(t => Id(t) == boarderId), commands,
                nativeInteractableOwnerIsTransporter = ReferenceEquals(ThingOwnerUtility.TryGetInnerInteractableThingOwner(gate), transporter.innerContainer),
                boardingJob = boarder?.CurJobDef == JobDefOf.EnterTransporter,
                boarderSpawnedOnSource = boarder?.Spawned == true && boarder.Map == gate.Map,
                boarderArrived = boarder?.Spawned == true && boarder.Map == destination.Map,
                itemArrived = !string.IsNullOrEmpty(itemId) && destination.Map?.listerThings.AllThings.Any(t => Id(t) == itemId) == true,
                markerInBoarderInventory = marker != null && boarder?.inventory?.innerContainer.Contains(marker) == true,
                boarder = boarder == null ? null : new { id = Id(boarder), dead = boarder.Dead, downed = boarder.Downed,
                    spawned = boarder.Spawned, mapId = boarder.MapHeld?.uniqueID, job = boarder.CurJobDef?.defName,
                    targetA = boarder.CurJob?.targetA.Thing?.GetUniqueLoadID(), targetB = boarder.CurJob?.targetB.Thing?.GetUniqueLoadID(),
                    parentHolder = boarder.ParentHolder?.GetType().FullName, inventoryIds = boarder.inventory?.innerContainer.Select(Id).ToArray() },
                transit, cargo, limits = "Read-only sequential samples; pause for exact boundaries. Native manifest references, transporter deep ownership, gate pending ownership and queue aliases differ. Commands are observed at this actual instant, not invented cancellation availability." };
        });
    }

    [Tool("rimgate/prepare_transporter_fixture", Description = "Stage two different loose guns and healthy source operators on the owned mature transit fixture. Does not assign a manifest, start hauling, admit or cancel anything.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath,
        string expectedModRoot, string sourceGateId, int destinationParentId, string pawnId, string closerId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        var gate = map.listerThings.AllThings.OfType<Building_Gate>().Single(g => Id(g) == sourceGateId);
        var destination = Find.WorldObjects.MapParents.OfType<WorldObject_GateTransitSite>()
            .Single(s => s.ID == destinationParentId && s.SiteName == "Rimgate transit destination");
        var transporter = gate.Transporter;
        if (gate.Position != new IntVec3(110, 0, 130) || gate.ConnectedDHD?.Position != new IntVec3(110, 0, 134)
            || !gate.IsActive || gate.IsReceivingGate || gate.IsOpeningQueued || gate.TicksSinceOpened < 150
            || gate.IsIrisActivated || gate.ConnectedGate == null || gate.ConnectedGate.IsIrisActivated
            || gate.ConnectedGate.Map != destination.Map || gate.GetDirectlyHeldThings().Count != 0
            || gate.ConnectedGate.GetDirectlyHeldThings().Count != 0 || transporter == null
            || transporter.LoadingInProgressOrReadyToLaunch || transporter.innerContainer.Count != 0)
            throw new InvalidOperationException("Use the exact mature reciprocal open-iris empty transit source before native loading.");
        var pawn = map.mapPawns.FreeColonistsSpawned.Single(p => Id(p) == pawnId);
        var closer = map.mapPawns.FreeColonistsSpawned.Single(p => Id(p) == closerId);
        if (pawn == closer || new[] { pawn, closer }.Any(p => p.Dead || p.Downed || p.InMentalState)
            || pawn.WorkTagIsDisabled(WorkTags.Hauling))
            throw new InvalidOperationException("Use separate healthy source colonists and a hauling-capable loader.");
        var hostiles = map.listerThings.AllThings.OfType<Pawn>().Where(p => p.Spawned && p.Map == map && !p.Dead
            && p.Faction != Faction.OfPlayer && !p.IsColonist && !p.IsPrisonerOfColony && !p.IsSlaveOfColony
            && (p.Faction?.HostileTo(Faction.OfPlayer) == true || p.HostileTo(Faction.OfPlayer))).ToArray();
        var removedHostileIds = hostiles.Select(Id).ToArray();
        foreach (var hostile in hostiles) hostile.Destroy(DestroyMode.Vanish);
        foreach (var cell in new CellRect(112, 119, 29, 18).ClipInsideMap(map))
        {
            map.roofGrid.SetRoof(cell, null);
            foreach (var thing in cell.GetThingList(map).ToArray())
                if (thing is not Pawn && thing is not Building_Gate && thing is not Building_DHD)
                    thing.Destroy(DestroyMode.Vanish);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        }
        foreach (var colonist in map.mapPawns.FreeColonistsSpawned.ToArray())
        {
            colonist.jobs.StopAll();
            colonist.drafter.Drafted = false;
            colonist.pather.StopDead();
            colonist.Position = colonist == pawn ? new IntVec3(127, 0, 134)
                : colonist == closer ? new IntVec3(111, 0, 134) : new IntVec3(119, 0, 108);
            var wait = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
            wait.expiryInterval = 60000;
            wait.playerForced = true;
            colonist.jobs.TryTakeOrderedJob(wait, JobTag.Misc);
        }
        pawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
        var first = GenSpawn.Spawn(DefDatabase<ThingDef>.GetNamed("Gun_Autopistol"), new IntVec3(126, 0, 134), map);
        var second = GenSpawn.Spawn(DefDatabase<ThingDef>.GetNamed("Gun_BoltActionRifle"), new IntVec3(138, 0, 122), map);
        first.SetForbidden(false, false);
        second.SetForbidden(false, false);
        // Native home-map manifests include loose items only inside Home or storage.
        // These two cells are eligibility inputs, not loaded contents or manifest entries.
        map.areaManager.Home[first.Position] = true;
        map.areaManager.Home[second.Position] = true;
        map.fogGrid.ClearAllFog();
        var sendable = TransporterUtility.AllSendableItems(new List<CompTransporter> { transporter }, map).ToArray();
        return new { success = true, sourceGateId, destinationParentId, pawnId, closerId, sourceMapId = map.uniqueID,
            firstId = Id(first), secondId = Id(second), firstDef = first.def.defName, secondDef = second.def.defName,
            homeAtFirst = map.areaManager.Home[first.Position], homeAtSecond = map.areaManager.Home[second.Position],
            firstNativeSendable = sendable.Any(t => ReferenceEquals(t, first)),
            secondNativeSendable = sendable.Any(t => ReferenceEquals(t, second)),
            removedHostileIds, limits = "Only loose two-def cargo, their two Home cells, approaches, hauling eligibility and known hostile-guard cleanup are inputs. Native AllSendableItems membership is observed, not inserted. Real Load UI must assign both quantities; native hauling must perform every owner/manifest transition. No instant-load, admission, cancellation, job-start or gate-state API is called." };
    });

    [Tool("rimgate/transporter_status", Description = "Read native gate transporter manifest/group/contents, loader job/carry and both exact cargo owners using the existing transit observer. Does not acknowledge a manifest or rescue unqueued pending contents.")]
    public static async Task<object> Status(IRimBridgeContext context, string expectedConfigPath,
        string expectedModRoot, string sourceGateId, int destinationParentId, string pawnId, string firstId, string secondId)
    {
        var first = await TransitProbe.Status(context, sourceGateId, destinationParentId, pawnId, firstId);
        var second = await TransitProbe.Status(context, sourceGateId, destinationParentId, pawnId, secondId);
        return await context.MainThread.InvokeAsync<object>(() =>
        {
            Guard(expectedConfigPath, expectedModRoot);
            var gate = Find.Maps.SelectMany(m => m.listerThings.AllThings.OfType<Building_Gate>()).Single(g => Id(g) == sourceGateId);
            var destination = Find.WorldObjects.MapParents.Single(p => p.ID == destinationParentId);
            var pawn = gate.Map.mapPawns.FreeColonistsSpawned.SingleOrDefault(p => Id(p) == pawnId);
            var transporter = gate.Transporter;
            var nativeOwner = ThingOwnerUtility.TryGetInnerInteractableThingOwner(gate);
            return new { success = true, tick = Find.TickManager.TicksGame,
                nativeInteractableOwnerIsTransporter = ReferenceEquals(nativeOwner, transporter.innerContainer),
                nativeInteractableOwnerIsPending = ReferenceEquals(nativeOwner, gate.GetDirectlyHeldThings()),
                nativeOwner = nativeOwner == null ? null : new { type = nativeOwner.GetType().FullName,
                    ownerType = nativeOwner.Owner?.GetType().FullName, count = nativeOwner.Count,
                    ids = nativeOwner.Select(Id).ToArray() },
                gateActive = gate.IsActive, gateLoading = gate.GateIsLoading, groupId = transporter.groupID,
                loading = transporter.LoadingInProgressOrReadyToLaunch,
                remaining = transporter.leftToLoad?.Sum(t => t.CountToTransfer) ?? 0,
                manifest = transporter.leftToLoad?.Select(t => new { count = t.CountToTransfer,
                    things = t.things.Where(thing => thing != null).Select(thing => new { id = Id(thing),
                        defName = thing.def.defName, count = thing.stackCount, spawned = thing.Spawned }).ToArray() }).ToArray(),
                transporterCount = transporter.innerContainer.Count,
                transporterContents = transporter.innerContainer.Select(t => new { id = Id(t), defName = t.def.defName,
                    count = t.stackCount, parentHolder = t.ParentHolder?.GetType().FullName }).ToArray(),
                commands = gate.GetGizmos().OfType<Command>().Select(c => new {
                    label = ColoredText.StripTags(c.Label), disabled = c.Disabled }).ToArray(),
                pending = gate.GetDirectlyHeldThings().Select(Id).ToArray(),
                loader = pawn == null ? null : new { id = Id(pawn), dead = pawn.Dead, downed = pawn.Downed,
                    job = pawn.CurJobDef?.defName, targetA = pawn.CurJob?.targetA.Thing?.GetUniqueLoadID(),
                    targetB = pawn.CurJob?.targetB.Thing?.GetUniqueLoadID(), count = pawn.CurJob?.count,
                    hasCarriedThing = pawn.carryTracker?.CarriedThing != null,
                    carriedId = pawn.carryTracker?.CarriedThing?.GetUniqueLoadID() },
                firstCarried = pawn?.carryTracker?.CarriedThing?.GetUniqueLoadID() == firstId,
                secondCarried = pawn?.carryTracker?.CarriedThing?.GetUniqueLoadID() == secondId,
                secondSpawnedOnSource = gate.Map.listerThings.AllThings.Any(t => t.Spawned && Id(t) == secondId),
                firstArrived = destination.Map?.listerThings.AllThings.Any(t => Id(t) == firstId) == true,
                secondArrived = destination.Map?.listerThings.AllThings.Any(t => Id(t) == secondId) == true,
                first, second, limits = "Sequential read-only main-thread samples; pause for boundary assertions. Native transporter deep owner, manifest references and gate pending/queue aliases are distinct. No duplicate acknowledgements or outcome mutations." };
        });
    }
    private static string Id(Thing thing) => thing.GetUniqueLoadID();
    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected isolated 1.7 profile and private mod deployment.");
    }
}
