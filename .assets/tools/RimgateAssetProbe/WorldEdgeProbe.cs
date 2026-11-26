using System.Reflection;
using RimBridgeServer.Sdk;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Rimgate.AssetProbe;

// Ordinary equipment inputs and observations; actual jobs, incidents and world UI own outcomes.
public sealed class WorldEdgeProbe
{
    private static readonly FieldInfo QuestId = typeof(WorldObject_GateQuestSite).GetField("_questId", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo HeldGate = typeof(GameCondition_Gate).GetField("_heldGate", BindingFlags.NonPublic | BindingFlags.Instance)!;

    [Tool("rimgate/prepare_orbital_travel", Description = "Stage loose vanilla vacsuit/helmet and retain a separate source closer for a real discovered orbital quest. Does not wear gear, move pawns, dial, generate a map, transfer or change vacuum.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string sourceGateId, int destinationParentId, string pawnId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        var source = map.listerThings.AllThings.OfType<Building_Gate>().Single(g => Id(g) == sourceGateId);
        var site = Find.WorldObjects.MapParents.OfType<WorldObject_GateQuestSite>().Single(s => s.ID == destinationParentId);
        if (source.Position != new IntVec3(110, 0, 130) || source.IsActive || source.IsOpeningQueued || source.IsIrisActivated
            || source.ConnectedDHD?.Position != new IntVec3(110, 0, 134) || !site.Tile.LayerDef.isSpace
            || !GateUtil.ModificationEquipmentActive || source.GetDirectlyHeldThings().Count != 0)
            throw new InvalidOperationException("Use the owned inactive open-iris gate, powered orbital equipment and real discovered orbital quest.");
        var pawn = map.mapPawns.FreeColonistsSpawned.Single(p => Id(p) == pawnId);
        var closer = map.mapPawns.FreeColonistsSpawned.Where(p => p != pawn && !p.Dead && !p.Downed && !p.InMentalState)
            .OrderBy(p => p.thingIDNumber).First();
        if (pawn.Dead || pawn.Downed || pawn.InMentalState || pawn.apparel == null)
            throw new InvalidOperationException("Use a healthy actual source traveler who can wear ordinary apparel.");
        var suitCell = new IntVec3(116, 0, 132);
        var helmetCell = new IntVec3(116, 0, 134);
        foreach (var cell in new[] { suitCell, helmetCell })
            if (!cell.Standable(map) || cell.GetThingList(map).Any(t => t is Pawn || t.def.category == ThingCategory.Item))
                throw new InvalidOperationException("Use empty standable gear input cells in the owned prepared colony.");
        var suit = GenSpawn.Spawn(DefDatabase<ThingDef>.GetNamed("Apparel_Vacsuit"), suitCell, map);
        var helmet = GenSpawn.Spawn(DefDatabase<ThingDef>.GetNamed("Apparel_VacsuitHelmet"), helmetCell, map);
        suit.SetForbidden(false, false);
        helmet.SetForbidden(false, false);
        foreach (var actor in new[] { pawn, closer })
        {
            actor.jobs.StopAll();
            actor.drafter.Drafted = false;
            actor.pather.StopDead();
            var wait = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
            wait.expiryInterval = 60000;
            wait.playerForced = true;
            actor.jobs.TryTakeOrderedJob(wait, Verse.AI.JobTag.Misc);
        }
        return new { success = true, sourceGateId, destinationParentId, pawnId, closerId = Id(closer), sourceMapId = map.uniqueID,
            questId = (int)QuestId.GetValue(site)!, siteTile = site.Tile.tileId, siteLayer = site.Tile.LayerDef.defName,
            initialActiveQuestSites = GateUtil.ActiveQuestSiteCount,
            initialIgnoredTick = GameComponent_RimgateDwarfgateTracker.Get().LastIgnoredTick,
            suitId = Id(suit), helmetId = Id(helmet), suitX = suitCell.x, suitZ = suitCell.z,
            helmetX = helmetCell.x, helmetZ = helmetCell.z,
            limits = "Only loose vanilla protection and ordinary source wait inputs. Real Wear jobs must equip it; native VacuumResistance, exposure, real discovery/dialing/entry and save/load are observed independently." };
    });

    [Tool("rimgate/world_edge_status", Description = "Read tracked traveler, native vacuum protection/exposure, actual quest site/map/address/history and spawned loot/hostile/incident state. No quest completion, site removal, rewards, vacuum flags or gameplay state changes.")]
    public static Task<object> Status(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string sourceGateId, int destinationParentId, string pawnId, string suitId = "", string helmetId = "")
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var all = AllThings();
        var pawn = all.OfType<Pawn>().SingleOrDefault(p => Id(p) == pawnId);
        var source = all.OfType<Building_Gate>().SingleOrDefault(g => Id(g) == sourceGateId);
        var site = Find.WorldObjects.MapParents.OfType<WorldObject_GateQuestSite>().SingleOrDefault(s => s.ID == destinationParentId);
        var map = site?.Map;
        var receiver = map?.listerThings.AllThings.OfType<Building_Gate>().SingleOrDefault();
        var tracker = GameComponent_RimgateDwarfgateTracker.Get();
        var worn = pawn?.apparel?.WornApparel;
        return new { success = true, tick = Find.TickManager.TicksGame, source = Gate(source), receiver = Gate(receiver),
            activeQuestSites = GateUtil.ActiveQuestSiteCount, addresses = GateUtil.WorldComp.AddressList.Select(t => new { tile = t.tileId, layer = t.LayerDef.defName }).ToArray(),
            site = site == null ? null : new { id = site.ID, label = site.Label, questId = (int)QuestId.GetValue(site)!,
                tile = site.Tile.tileId, layer = site.Tile.LayerDef.defName, site.HasMap, mapId = map?.uniqueID,
                site.WasEverVisited, designation = GateUtil.GetGateDesignation(site.Tile),
                blockingPawns = map?.mapPawns.AnyPawnBlockingMapRemoval == true,
                parts = site.parts.Select(p => p.def.defName).ToArray(),
                gizmos = site.GetGizmos().OfType<Command>().Select(g => new { label = g.Label, disabled = g.Disabled, reason = g.disabledReason }).ToArray() },
            pawn = pawn == null ? null : new { id = Id(pawn), pawn.Dead, pawn.Downed, pawn.Drafted, pawn.Spawned,
                mapId = pawn.MapHeld?.uniqueID, x = pawn.PositionHeld.x, z = pawn.PositionHeld.z,
                job = pawn.CurJobDef?.defName, vacuumResistance = pawn.GetStatValue(DefDatabase<StatDef>.GetNamed("VacuumResistance")),
                roomVacuum = pawn.Spawned ? pawn.GetRoom()?.Vacuum : null,
                harmedByVacuum = pawn.HarmedByVacuum, suitWorn = worn?.Any(t => Id(t) == suitId) == true,
                helmetWorn = worn?.Any(t => Id(t) == helmetId) == true,
                apparel = worn?.Select(t => new { id = Id(t), defName = t.def.defName, hp = t.HitPoints }).ToArray(),
                health = pawn.health.hediffSet.hediffs.Select(h => new { defName = h.def.defName, severity = h.Severity }).ToArray() },
            quests = Find.QuestManager.QuestsListForReading.Select(q => new { id = q.id, root = q.root?.defName, state = q.State.ToString(), q.name,
                parts = q.PartsListForReading.Select(p => p.GetType().FullName).ToArray() }).ToArray(),
            tracker.LastExpeditionTick, tracker.LastIgnoredTick,
            loot = map == null ? null : all.Where(t => t.def.category == ThingCategory.Item && t.MapHeld == map)
                .Select(t => new { id = Id(t), defName = t.def.defName, count = t.stackCount,
                    spawned = t.Spawned, holder = t.ParentHolder?.GetType().FullName,
                    x = t.PositionHeld.x, z = t.PositionHeld.z }).ToArray(),
            hostiles = Find.CurrentMap.mapPawns.AllPawnsSpawned.Where(p => p.HostileTo(Faction.OfPlayer))
                .Select(p => new { id = Id(p), defName = p.def.defName, faction = p.Faction?.def.defName,
                    job = p.CurJobDef?.defName, duty = p.mindState.duty?.def.defName,
                    lordJob = p.GetLord()?.LordJob?.GetType().FullName }).ToArray(),
            conditions = Find.CurrentMap.GameConditionManager.ActiveConditions.Select(c => new { id = c.uniqueID, defName = c.def.defName, c.TicksLeft,
                heldGateId = c is GameCondition_Gate condition ? (HeldGate.GetValue(condition) as Building_Gate)?.GetUniqueLoadID() : null,
                temperatureOffset = c.TemperatureOffset(), skyLerp = c.SkyTargetLerpFactor(Find.CurrentMap) }).ToArray(),
            skyfallers = Find.CurrentMap.listerThings.AllThings.OfType<Skyfaller>()
                .Select(t => new { id = Id(t), defName = t.def.defName, x = t.Position.x, z = t.Position.z }).ToArray(),
            naquadah = Find.CurrentMap.listerThings.AllThings.Where(t => t.def == RimgateDefOf.Rimgate_MineableNaquadah)
                .Select(t => new { id = Id(t), x = t.Position.x, z = t.Position.z }).ToArray(),
            limits = "Read-only loaded-state observations; world gizmo enumeration does not execute actions. Actual clothes/protection are separate from room vacuum and health. Loot existence is not pickup/reward completion, and hostile generation is not combat victory." };
    });

    [Tool("rimgate/select_quest_site", Description = "Select the exact real gate quest site in the world UI without invoking its abandon action or changing quest/site state.")]
    public static Task<object> SelectSite(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot, int destinationParentId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var site = Find.WorldObjects.MapParents.OfType<WorldObject_GateQuestSite>().Single(s => s.ID == destinationParentId);
        CameraJumper.TryJump(site);
        Find.WorldSelector.Select(site);
        return new { success = true, id = site.ID, limits = "World selection only; use the real UI control for abandonment." };
    });

    private static object Gate(Building_Gate gate) => gate == null ? null : new { id = Id(gate), mapId = gate.Map.uniqueID,
        x = gate.Position.x, z = gate.Position.z, gate.IsActive, gate.IsReceivingGate, gate.TicksSinceOpened,
        gate.ExternalHoldCount, irisClosed = gate.IsIrisActivated,
        dhd = gate.ConnectedDHD == null ? null : new { id = Id(gate.ConnectedDHD), x = gate.ConnectedDHD.Position.x, z = gate.ConnectedDHD.Position.z } };
    private static Thing[] AllThings()
    {
        var result = new List<Thing>();
        var visited = new HashSet<IThingHolder>(ReferenceEqualityComparer.Instance);
        void Walk(IThingHolder holder)
        {
            if (!visited.Add(holder)) return;
            if (holder.GetDirectlyHeldThings() is { } owner) result.AddRange(owner);
            var children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            foreach (var child in children) Walk(child);
        }
        foreach (var map in Find.Maps) Walk(map);
        return result.Distinct(ReferenceEqualityComparer.Instance).Cast<Thing>().ToArray();
    }
    private static string Id(Thing thing) => thing.GetUniqueLoadID();
    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7 || !Prefs.DevMode
            || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected isolated 1.7 profile and private deployment.");
    }
}
