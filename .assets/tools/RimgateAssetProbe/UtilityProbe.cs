using System.Reflection;
using RimBridgeServer.Sdk;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.AssetProbe;

// Inputs and observations only. Normal player menus/jobs operate the ZPMs and cart.
public sealed class UtilityProbe
{
    private static readonly IntVec3 HousingCell = new(126, 0, 114);
    private static readonly IntVec3 CartCell = new(133, 0, 111);

    [Tool("rimgate/prepare_zpm_edges", Description = "Stage a loose adjacent shroud and nearby phase diverter beside the owned utility housing. Research and empty facilities are inputs; ordinary ticks must establish links, power, recharge and suppression.")]
    public static Task<object> PrepareZpmEdges(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot, string housingId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        var housing = map.listerThings.AllThings.OfType<Building_ZPMHousing>().Single(h => Id(h) == housingId);
        if (housing.Position != HousingCell || housing.ZpmCount != 3)
            throw new InvalidOperationException("Use the owned full utility housing.");
        if (map.listerThings.AllThings.Any(t => t.def.defName is "Rimgate_SubspaceShroudEmitter" or "Rimgate_SubspacePhaseDiverter"))
            throw new InvalidOperationException("Facilities already exist; reload the pre-edge checkpoint.");
        var shroud = Spawn("Rimgate_SubspaceShroudEmitter", new IntVec3(125, 0, 114), map);
        var diverter = Spawn("Rimgate_SubspacePhaseDiverter", new IntVec3(129, 0, 116), map);
        if (!housing.Spawned || !shroud.Spawned || !diverter.Spawned)
            throw new InvalidOperationException("Facility inputs must not replace the housing.");
        return new { success = true, shroudId = Id(shroud), diverterId = Id(diverter), shroudCell = Cell(shroud.Position), diverterCell = Cell(diverter.Position) };
    });

    [Tool("rimgate/prepare_second_zpm_shroud", Description = "Add one ordinary empty shroud at (125,115) beside the owned full housing and existing first shroud. Preserves original objects; real power initialization, facility linking and flick jobs must establish suppression.")]
    public static Task<object> PrepareSecondShroud(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot, string housingId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        var housing = map.listerThings.AllThings.OfType<Building_ZPMHousing>().Single(h => Id(h) == housingId);
        var def = DefDatabase<ThingDef>.GetNamed("Rimgate_SubspaceShroudEmitter");
        var first = map.listerThings.ThingsOfDef(def).SingleOrDefault(t => t.Position == new IntVec3(125, 0, 114));
        var cell = new IntVec3(125, 0, 115);
        if (!housing.Spawned || housing.Position != HousingCell || housing.ZpmCount != 3 || first == null
            || map.listerThings.ThingsOfDef(def).Count != 1 || !cell.InBounds(map)
            || cell.GetThingList(map).Any(t => t.def.defName is not ("PowerConduit" or "HiddenConduit"))
            || !GenSpawn.CanSpawnAt(def, cell, map, Rot4.North, true)
            || cell.GetThingList(map).Any(t => GenSpawn.SpawningWipes(def, t.def, true)))
            throw new InvalidOperationException("Use the owned full housing, first shroud at (125,114), and untouched empty second cell (125,115).");
        var originals = map.listerThings.AllThings.Select(t => (Thing: t, t.Spawned, t.Position, t.HitPoints)).ToArray();
        var second = Spawn(def.defName, cell, map);
        if (originals.Any(p => p.Thing.Destroyed || p.Thing.Spawned != p.Spawned || p.Thing.Position != p.Position || p.Thing.HitPoints != p.HitPoints))
        {
            second.Destroy(DestroyMode.Vanish);
            throw new InvalidOperationException("Second-shroud input changed an original object.");
        }
        return new { success = true, housingId, firstShroudId = Id(first), secondShroudId = Id(second),
            secondCell = Cell(second.Position), limits = "One ordinary shroud input only. No PowerOn, suppression, quest, link or flick state is forced; use real flick jobs and ordinary ticks." };
    });

    [Tool("rimgate/prepare_utilities_fixture", Description = "Stage three charged ZPM inputs, an empty connected housing/lamp and a steel wheelbarrow with loose unique cargo in the owned smoke colony. Does not insert, load, push or unload anything; use normal player UI and jobs.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        var gate = map.listerThings.AllThings.OfType<Building_Gate>()
            .SingleOrDefault(g => g.Position == new IntVec3(110, 0, 130));
        if (gate == null || gate.IsActive || gate.IsOpeningQueued || gate.IsIrisActivated
            || gate.ConnectedDHD?.Position != new IntVec3(110, 0, 134))
            throw new InvalidOperationException("Load the owned smoke save with its inactive open-iris gate and linked controller.");
        if (map.listerThings.AllThings.Any(t => t is Building_ZPMHousing or Building_MobileContainer))
            throw new InvalidOperationException("Reload the owned smoke save before preparing utilities again.");
        var pawn = map.mapPawns.FreeColonistsSpawned
            .Where(p => !p.Dead && !p.Downed && !p.InMentalState && !p.WorkTagIsDisabled(WorkTags.Hauling))
            .OrderBy(p => p.thingIDNumber).FirstOrDefault()
            ?? throw new InvalidOperationException("A healthy colonist capable of hauling is required.");
        int index = 0;
        foreach (var colonist in map.mapPawns.FreeColonistsSpawned.ToArray())
        {
            colonist.jobs.StopAll();
            colonist.drafter.Drafted = false;
            colonist.pather.StopDead();
            colonist.Position = new IntVec3(118, 0, 108 + index++);
            var wait = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
            wait.expiryInterval = 60000;
            wait.playerForced = true;
            colonist.jobs.TryTakeOrderedJob(wait, JobTag.Misc);
        }
        // Clear only this isolated smoke fixture's utility/work runway, away from the gate.
        foreach (var cell in new CellRect(119, 109, 28, 17).ClipInsideMap(map))
        {
            map.roofGrid.SetRoof(cell, null);
            foreach (var thing in cell.GetThingList(map).ToArray())
                if (thing is not Pawn) thing.Destroy(DestroyMode.Vanish);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        }
        // This owned input colony contains unrelated ancient guards under fog.
        // Remove only its initially hostile spawned actors; never player/colony pawns.
        var initialMapPawns = map.listerThings.AllThings.OfType<Pawn>().ToArray();
        var hostileCleanupCandidates = initialMapPawns.Select(p => new
        {
            id = p.GetUniqueLoadID(), defName = p.def.defName, spawned = p.Spawned, onMap = p.Map == map,
            factionId = p.Faction?.GetUniqueLoadID(), factionHostile = p.Faction?.HostileTo(Faction.OfPlayer) == true,
            pawnHostile = p.HostileTo(Faction.OfPlayer), colonist = p.IsColonist,
            prisoner = p.IsPrisonerOfColony, slave = p.IsSlaveOfColony
        }).ToArray();
        var removedHostiles = initialMapPawns.Where(p => p.Spawned && p.Map == map && !p.Dead
            && p.Faction != Faction.OfPlayer && !p.IsColonist && !p.IsPrisonerOfColony && !p.IsSlaveOfColony
            && (p.Faction?.HostileTo(Faction.OfPlayer) == true || p.HostileTo(Faction.OfPlayer))).ToArray();
        var removedHostileIds = removedHostiles.Select(p => p.GetUniqueLoadID()).ToArray();
        foreach (var hostile in removedHostiles) hostile.Destroy(DestroyMode.Vanish);
        map.fogGrid.ClearAllFog();
        pawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
        Find.ResearchManager.FinishProject(RimgateDefOf.Rimgate_ZPMIntegration, false, pawn, false);
        Find.ResearchManager.FinishProject(DefDatabase<ResearchProjectDef>.GetNamed("Rimgate_ZPMHousing"), false, pawn, false);
        var housing = (Building_ZPMHousing)Spawn("Rimgate_ZPMHousing", HousingCell, map);
        housing.GetComp<CompPowerBattery>().SetStoredEnergyPct(0);
        // Batteries transmit power themselves. A conduit on their occupied cells
        // can uninstall/minify the fixture instead of connecting it.
        for (int x = 128; x <= 129; x++)
        {
            var cell = new IntVec3(x, 0, 114);
            if (cell.GetThingList(map).Any(t => t.TryGetComp<CompPower>()?.Props.transmitsPower == true))
                throw new InvalidOperationException("Do not place fixture conduits on an existing power transmitter.");
            Spawn("PowerConduit", cell, map);
        }
        var lamp = Spawn("StandingLamp", new IntVec3(129, 0, 114), map);
        var zpms = new List<Building_ZPM>();
        for (int i = 0; i < 3; i++)
        {
            var zpm = (Building_ZPM)Spawn("Rimgate_ZPM", new IntVec3(121 + i * 2, 0, 111), map);
            zpm.Battery.SetStoredEnergyPct(0.5f);
            zpm.SetDarkEnergyReserve(500);
            zpms.Add(zpm);
        }
        var cart = (Building_MobileContainer)Spawn("Rimgate_Wheelbarrow", CartCell, map, ThingDefOf.Steel);
        cart.AllowColonistsUseContents = false;
        var cargo = Spawn("Gun_Autopistol", new IntVec3(131, 0, 112), map);
        if (!housing.Spawned || !lamp.Spawned)
            throw new InvalidOperationException("Fixture placement must leave the housing and lamp spawned.");
        return new
        {
            success = true, removedHostileIds, hostileCleanupCandidates, mapId = map.uniqueID, pawnId = pawn.GetUniqueLoadID(),
            housingId = housing.GetUniqueLoadID(), zpmIds = zpms.Select(z => z.GetUniqueLoadID()).ToArray(),
            cartId = cart.GetUniqueLoadID(), cargoId = cargo.GetUniqueLoadID(), lampId = lamp.GetUniqueLoadID(),
            housingCell = Cell(HousingCell), cartCell = Cell(CartCell), cargoCell = Cell(cargo.Position),
            zpmCells = zpms.Select(z => Cell(z.Position)).ToArray(),
            pushDestination = Cell(new IntVec3(143, 0, 123)),
            snapshot = Snapshot(pawn.GetUniqueLoadID(), housing.GetUniqueLoadID(), cargo.GetUniqueLoadID(),
                string.Join(",", zpms.Select(z => z.GetUniqueLoadID()))),
            limits = "Research, seeded loose charges, empty buildings, unique loose cargo and same-map positions are fixture inputs. Normal menus/jobs must insert/eject/load/push/unload. Wheelbarrow has no fuel; recharge facilities, fuel variants and gate transit require separate checks. Cart conversion creates a new cart ID; cargo identity and ownership are the continuity oracle. Initially hostile spawned guards are removed before unfogging; no colony pawn is removed."
        };
    });

    [Tool("rimgate/utilities_status", Description = "Read ZPM/housing charge and exact cargo ownership across map, pawn, proxy and cart after normal jobs or save/load. Reacquires objects by saved IDs; does not complete jobs or mutate containers.")]
    public static Task<object> Status(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string pawnId, string housingId, string cargoId, string zpmIds)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        return Snapshot(pawnId, housingId, cargoId, zpmIds);
    });

    private static object Snapshot(string pawnId, string housingId, string cargoId, string zpmIds)
    {
        var occurrences = new List<Occurrence>();
        var visited = new HashSet<IThingHolder>(ReferenceEqualityComparer.Instance);
        foreach (var map in Find.Maps) Walk(map, "map:" + map.uniqueID, visited, occurrences);
        Walk(Find.World, "world", visited, occurrences);
        var all = occurrences.Select(o => o.Thing).Distinct(ReferenceEqualityComparer.Instance).Cast<Thing>().ToArray();
        var pawn = all.OfType<Pawn>().SingleOrDefault(t => Id(t) == pawnId);
        var housing = all.OfType<Building_ZPMHousing>().SingleOrDefault(t => Id(t) == housingId);
        var battery = housing?.GetComp<CompPowerBattery>();
        var mapNow = housing?.Map ?? pawn?.Map;
        return new
        {
            success = true, tick = Find.TickManager.TicksGame,
            currentMapId = Find.CurrentMap?.uniqueID,
            fixtureLampSpawned = mapNow?.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("StandingLamp"))
                .Any(l => l.Spawned && l.Position == new IntVec3(129, 0, 114)) == true,
            selectedPawnIds = Find.Selector.SelectedPawns.Select(Id).ToArray(),
            housingClickThings = housing?.Spawned == true ? housing.Position.GetThingList(housing.Map)
                .Select(t => new { id = Id(t), def = t.def.defName, type = t.GetType().FullName }).ToArray() : null,
            pawn = pawn == null ? null : new
            {
                id = Id(pawn), mapId = pawn.Map?.uniqueID, cell = Cell(pawn.Position), dead = pawn.Dead,
                downed = pawn.Downed, drafted = pawn.Drafted, job = pawn.CurJobDef?.defName,
                jobTargetA = pawn.CurJob?.targetA.Thing?.GetUniqueLoadID(),
                jobTargetB = pawn.CurJob?.targetB.Thing?.GetUniqueLoadID(),
                toilIndex = pawn.jobs.curDriver?.CurToilIndex,
                carriedId = pawn.carryTracker?.CarriedThing?.GetUniqueLoadID(),
                pushingHediffCount = pawn.health.hediffSet.hediffs.Count(h => h.def == RimgateDefOf.Rimgate_PushingCart),
                attachedCartVisual = pawn.TryGetComp<CompAttachBase>()?.GetAttachment(RimgateDefOf.Rimgate_PushedCartVisual) != null
            },
            research = new { integration = ResearchUtil.ZPMIntegrationComplete, recharge = ResearchUtil.ParallelSubspaceCouplingComplete },
            housing = housing == null ? null : new
            {
                id = Id(housing), cell = Cell(housing.Position), spawned = housing.Spawned,
                mapId = housing.Map?.uniqueID, count = housing.ZpmCount,
                tickerType = housing.def.tickerType.ToString(),
                hasInteractionCell = housing.def.hasInteractionCell,
                interactionCell = housing.Spawned ? Cell(housing.InteractionCell) : null,
                interactionWalkable = housing.Spawned && housing.InteractionCell.Walkable(housing.Map),
                interactionStandable = housing.Spawned && housing.InteractionCell.Standable(housing.Map),
                canReachInteraction = housing.Spawned && pawn != null && pawn.CanReach(housing, PathEndMode.InteractionCell, Danger.Deadly),
                canReachTouch = housing.Spawned && pawn != null && pawn.CanReach(housing, PathEndMode.Touch, Danger.Deadly),
                canReachInteractionOnCell = housing.Spawned && pawn != null && pawn.CanReach(housing.InteractionCell, PathEndMode.OnCell, Danger.Deadly),
                directMenu = pawn == null || !housing.Spawned ? null : housing.GetFloatMenuOptions(pawn)
                    .Select(option => new { label = option.Label, disabled = option.Disabled }).ToArray(),
                canAccept = housing.CanAcceptZpm, storedEnergy = battery?.StoredEnergy,
                physicalCapacity = battery?.Props.storedEnergyMax, effectiveCapacity = housing.ZpmCount * 48000,
                darkReserve = (int)Read(housing, "_darkEnergyReserve"), maxDark = (int)Read(housing, "_maxDarkEnergy"),
                broadcasting = (bool)Read(housing, "_isBroadcasting"), connected = battery?.PowerNet != null,
                netGainRate = battery?.PowerNet?.CurrentEnergyGainRate(),
                facilities = housing.GetComp<CompAffectedByFacilities>()?.LinkedFacilitiesListForReading.Select(t => new { id = Id(t), def = t.def.defName, powerOn = t.TryGetComp<CompPowerTrader>()?.PowerOn }).ToArray(),
                heldIds = housing.GetDirectlyHeldThings().Select(Id).ToArray()
            },
            zpms = zpmIds.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(id => new { id, ownership = Tracked(id, occurrences), state = ZpmState(all.OfType<Building_ZPM>().SingleOrDefault(z => Id(z) == id)) }).ToArray(),
            cargo = Tracked(cargoId, occurrences),
            cartsCount = all.OfType<Building_MobileContainer>().Count(),
            carts = all.OfType<Building_MobileContainer>().Select(c => new
            {
                id = Id(c), spawned = c.Spawned, mapId = c.Map?.uniqueID, cell = Cell(c.Position),
                def = c.def.defName, stuff = c.Stuff?.defName, hitPoints = c.HitPoints,
                loading = c.LoadingInProgress, remaining = c.LeftToLoad?.Sum(t => t.CountToTransfer) ?? 0,
                heldIds = c.InnerContainer.Select(Id).ToArray(), heldCount = c.InnerContainer.Count,
                mass = c.MassUsage, capacity = c.MassCapacity,
                allowUseContents = c.AllowColonistsUseContents, hasPushTarget = c.HasPushTarget,
                fuel = c.Refuelable?.Fuel, fuelOk = c.FuelOK,
                autoRefuel = c.Refuelable?.allowAutoRefuel,
                baseFuelPerTick = c.ConsumptionRatePerTick,
                contentsDontTick = c.InnerContainer.dontTickContents,
                contents = c.InnerContainer.Select(t => new { id = Id(t), def = t.def.defName, count = t.stackCount,
                    rotProgress = t.TryGetComp<CompRottable>()?.RotProgress, rotDisabled = t.TryGetComp<CompRottable>()?.disabled }).ToArray()
            }).ToArray(),
            proxiesCount = all.OfType<Thing_MobileCartProxy>().Count(),
            proxies = all.OfType<Thing_MobileCartProxy>().Select(p => new
            {
                id = Id(p), parentHolder = p.ParentHolder?.GetType().FullName,
                heldIds = p.InnerContainer.Select(Id).ToArray(), heldCount = p.InnerContainer.Count,
                savedDef = p.SavedDef?.defName,
                savedStuff = p.SavedStuff?.defName, savedHitPoints = p.SavedHitPoints,
                allowUseContents = p.SavedUseContentsSetting, fuel = p.PushingFuel, fuelPerTick = p.FuelPerTick,
                autoRefuel = p.SavedAutoRefuel,
                destination = Cell(p.PushDestination)
            }).ToArray(),
            lamp = mapNow?.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("StandingLamp"))
                .Select(l => new { id = Id(l), spawned = l.Spawned, cell = Cell(l.Position),
                    powerOn = l.TryGetComp<CompPowerTrader>()?.PowerOn }).ToArray(),
            tracker = mapNow == null ? null : new
            {
                activeCount = (int)Read(mapNow.GetComponent<MapComponent_ZpmRaidTracker>(), "_activeZpmCount"),
                questId = (int)Read(mapNow.GetComponent<MapComponent_ZpmRaidTracker>(), "_questId"),
                suppressed = mapNow.GetComponent<MapComponent_ZpmRaidTracker>().SuppressionActive
            },
            threatPointsNow = mapNow == null ? (float?)null : StorytellerUtility.DefaultThreatPointsNow(mapNow),
            shrouds = mapNow?.listerThings.AllThings.Where(t => t.def.defName == "Rimgate_SubspaceShroudEmitter").Select(t => new { id = Id(t), powerOn = t.TryGetComp<CompPowerTrader>()?.PowerOn,
                isSuppressing = t.TryGetComp<Comp_ZpmSignalScrambler>()?.IsSuppressing,
                flickOn = t.TryGetComp<CompFlickable>()?.SwitchIsOn,
                links = t.TryGetComp<CompFacility>()?.LinkedBuildings.Select(Id).ToArray(), inspect = t.GetInspectString() }).ToArray(),
            trackerCaches = all.Where(t => t is Building_ZPM or Building_ZPMHousing || t.TryGetComp<Comp_ZpmSignalScrambler>() != null)
                .Select(TrackerCache).ToArray(),
            protectionQuests = Find.QuestManager.QuestsListForReading.Where(q => q.root == RimgateDefOf.Rimgate_ProtectZPM)
                .Select(q => new { id = q.id, state = q.State.ToString(), raids = q.PartsListForReading.OfType<QuestPart_RandomFactionRaid>()
                    .Select(p => new { targetDefs = p.TargetDefs?.Select(d => d.defName).ToArray(), requiresBroadcastingZpm = p.RequireBroadcastingZpm,
                        pointsRangeMin = p.pointsRange.min, pointsRangeMax = p.pointsRange.max,
                        useCurrentThreatPoints = p.useCurrentThreatPoints, currentThreatPointsFactor = p.currentThreatPointsFactor,
                        actualFactionDef = p.faction?.def.defName,
                        actualFactionMinCombatPoints = p.faction == null ? (float?)null : p.faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat),
                        letterKey = p.UseLetterKey, signal = p.inSignal, actualAttackTargetIds = p.attackTargets?.Select(Id).ToArray(),
                        actualLetterLabel = p.customLetterLabel, actualLetterText = p.customLetterText }).ToArray() }).ToArray(),
            lords = mapNow?.lordManager.lords.Select(l => new { job = l.LordJob?.GetType().FullName,
                pawns = l.ownedPawns.Select(p => new { id = Id(p), kind = p.kindDef.defName, job = p.CurJobDef?.defName,
                    targetA = p.CurJob?.targetA.Thing?.GetUniqueLoadID(), targetB = p.CurJob?.targetB.Thing?.GetUniqueLoadID() }).ToArray() }).ToArray(),
            letters = Find.LetterStack.LettersListForReading.Select(l => l.Label.ToString()).ToArray(),
            limits = "Observation only; no jobs, charge transfers, queue clearing or tracker rebuilding. Occurrences count deep owners, not job target references. Map-root traversal includes pawn carry/inventory and nested cart/proxy contents."
        };
    }

    private static object ZpmState(Building_ZPM zpm) => zpm == null ? null : new
    {
        id = Id(zpm), spawned = zpm.Spawned, mapId = zpm.Map?.uniqueID, cell = Cell(zpm.Position),
        storedEnergy = zpm.Battery.StoredEnergy, darkReserve = zpm.DarkEnergyReserve,
        maxDark = zpm.MaxDarkEnergy, broadcasting = zpm.IsBroadcasting,
        connected = zpm.Battery.PowerNet != null, canRecharge = zpm.CanRecharge
    };

    private static object TrackerCache(Thing thing)
    {
        // Read the field, not the lazy Tracker property: observation must not populate a missing cache.
        object owner = thing is Building_ZPM or Building_ZPMHousing ? thing : thing.TryGetComp<Comp_ZpmSignalScrambler>();
        var tracker = Read(owner, owner is Comp_ZpmSignalScrambler ? "_cachedTracker" : "_tracker") as MapComponent_ZpmRaidTracker;
        var cachedMap = tracker == null ? null : (Map)typeof(MapComponent).GetField("map", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(tracker);
        return new { id = Id(thing), def = thing.def.defName, spawned = thing.Spawned, mapId = thing.Map?.uniqueID,
            heldMapId = thing.MapHeld?.uniqueID, cachePresent = tracker != null, cachedTrackerMapId = cachedMap?.uniqueID,
            cacheMatchesSpawnedMap = tracker == null || !thing.Spawned ? (bool?)null : ReferenceEquals(cachedMap, thing.Map),
            cacheMatchesHeldMap = tracker == null || thing.MapHeld == null ? (bool?)null : ReferenceEquals(cachedMap, thing.MapHeld) };
    }

    private static object Tracked(string id, List<Occurrence> occurrences)
    {
        var hits = occurrences.Where(o => Id(o.Thing) == id).ToArray();
        return new
        {
            id, ownershipOccurrenceCount = hits.Length,
            distinctObjectCount = hits.Select(o => o.Thing).Distinct(ReferenceEqualityComparer.Instance).Count(),
            locations = hits.Select(o => new { location = o.Location, spawned = o.Thing.Spawned,
                rotProgress = o.Thing.TryGetComp<CompRottable>()?.RotProgress,
                rotDisabled = o.Thing.TryGetComp<CompRottable>()?.disabled,
                parentHolder = o.Thing.ParentHolder?.GetType().FullName }).ToArray()
        };
    }

    private sealed record Occurrence(Thing Thing, string Location);
    private static void Walk(IThingHolder holder, string location, HashSet<IThingHolder> visited, List<Occurrence> result)
    {
        if (!visited.Add(holder)) return;
        var here = location + "/" + (holder is Thing thing ? Id(thing) : holder.GetType().Name);
        var contents = holder.GetDirectlyHeldThings();
        if (contents != null)
            foreach (var held in contents) result.Add(new Occurrence(held, here));
        var children = new List<IThingHolder>();
        holder.GetChildHolders(children);
        foreach (var child in children)
            Walk(child, here, visited, result);
    }

    private static Thing Spawn(string def, IntVec3 cell, Map map, ThingDef stuff = null)
    {
        var thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(def), stuff);
        if (thing.def.CanHaveFaction) thing.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(thing, cell, map);
        thing.SetForbidden(false, false);
        return thing;
    }
    private static string Id(Thing thing) => thing.GetUniqueLoadID();
    private static object Cell(IntVec3 cell) => new { x = cell.x, z = cell.z };
    private static object Read(object owner, string field) => owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner);
    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected isolated 1.7 profile and mod deployment.");
    }
}
