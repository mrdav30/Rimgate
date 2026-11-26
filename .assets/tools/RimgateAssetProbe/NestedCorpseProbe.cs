using System.Reflection;
using RimBridgeServer.Sdk;
using RimWorld;
using Verse;

namespace Rimgate.AssetProbe;

// Input creation and ownership observations only. Native loading/travel owns all outcomes.
public sealed class NestedCorpseProbe
{
    private static readonly FieldInfo StoredThings = typeof(WorldObject_GateTransitSite)
        .GetField("_storedThings", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo WasLooted = typeof(WorldObject_GateTransitSite)
        .GetField("_wasLooted", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Tool("rimgate/prepare_nested_corpse", Description = "Create one new loose fixture corpse and unique nested inventory pistol beside an existing empty owned cart. Does not load, move existing actors/cart, dial, transfer or snapshot anything.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath,
        string expectedModRoot, string sourceGateId, int destinationParentId, string pawnId,
        string cartId, bool seedBurn = true)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        var gate = map.listerThings.AllThings.OfType<Building_Gate>().Single(g => Id(g) == sourceGateId);
        var site = ResolveSite(destinationParentId);
        var receiver = site.Map?.listerThings.AllThings.OfType<Building_Gate>().SingleOrDefault();
        var cart = map.listerThings.AllThings.OfType<Building_MobileContainer>().Single(c => Id(c) == cartId);
        var pusher = map.mapPawns.FreeColonistsSpawned.Single(p => Id(p) == pawnId);
        if (gate.Position != new IntVec3(110, 0, 130) || gate.ConnectedDHD?.Position != new IntVec3(110, 0, 134)
            || !gate.IsActive || gate.IsReceivingGate || gate.IsOpeningQueued || gate.TicksSinceOpened < 150
            || gate.IsIrisActivated || receiver == null || !receiver.IsActive || !receiver.IsReceivingGate
            || receiver.IsIrisActivated || gate.ConnectedGate != receiver || receiver.ConnectedGate != gate
            || gate.GetDirectlyHeldThings().Count != 0 || receiver.GetDirectlyHeldThings().Count != 0
            || cart.def != RimgateDefOf.Rimgate_Wheelbarrow || cart.Stuff != ThingDefOf.Steel
            || cart.Faction != Faction.OfPlayer || cart.InnerContainer.Count != 0 || cart.LoadingInProgress
            || pusher.Dead || pusher.Downed || pusher.InMentalState || pusher.carryTracker.CarriedThing != null)
            throw new InvalidOperationException("Use the mature reciprocal empty owned transit link, empty steel wheelbarrow and healthy empty-handed pusher on its source map.");

        // Prefer the existing loose-pistol side, but never wipe it or any other object.
        var offsets = new[] { new IntVec3(-2, 0, 1), new IntVec3(-2, 0, -1), new IntVec3(2, 0, 1), new IntVec3(2, 0, -1) };
        var cell = offsets.Select(o => cart.Position + o).Where(c => c.InBounds(map) && c.Standable(map)
            && c.GetThingList(map).Count == 0 && c.DistanceToSquared(cart.Position) <= cart.Ext.SqrdLoadRadius)
            .DefaultIfEmpty(IntVec3.Invalid).First();
        if (!cell.IsValid || !cell.InBounds(map) || cell.GetThingList(map).Count != 0
            || cell.DistanceToSquared(cart.Position) > cart.Ext.SqrdLoadRadius)
            throw new InvalidOperationException("No strictly empty standable corpse-input cell beside the actual cart; preserve existing things and choose another input fixture.");
        var receiverPawns = receiver.Map.mapPawns.AllPawnsSpawned.ToArray();
        if (receiverPawns.Any(p => p.Faction == Faction.OfPlayer || p.IsColonist || p.IsPrisonerOfColony || p.IsSlaveOfColony))
            throw new InvalidOperationException("Prepare only before any colony/player/prisoner/slave pawn enters the receiving fixture.");
        // A fresh controlled destination excludes its initial wildlife as well as hostile guards.
        // Predatory wasps need not report HostileTo until an actual attack begins.
        var removedReceiverPawns = receiverPawns.Select(p => new { id = Id(p), defName = p.def.defName,
            faction = p.Faction?.GetUniqueLoadID(), factionDef = p.Faction?.def.defName }).ToArray();
        var receiverHazards = receiver.Map.listerThings.AllThings
            .Where(t => t.Spawned && t.def.defName == "WaspDroneTrap").ToArray();
        var removedReceiverHazards = receiverHazards.Select(t => new { id = Id(t), defName = t.def.defName }).ToArray();
        foreach (var initialPawn in receiverPawns) initialPawn.Destroy(DestroyMode.Vanish);
        foreach (var initialTrap in receiverHazards) initialTrap.Destroy(DestroyMode.Vanish);
        var subject = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
            canGeneratePawnRelations: false, fixedBiologicalAge: 25f, fixedChronologicalAge: 25f));
        subject.Name = new NameTriple("Rimgate", "Nested", "corpse input");
        GenSpawn.Spawn(subject, cell, map);
        string burnId = "";
        if (seedBurn)
        {
            var torso = subject.RaceProps.body.AllParts.First(p => p.def.defName == "Torso");
            var burn = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("Burn"), subject, torso);
            burn.Severity = 3f;
            subject.health.AddHediff(burn);
            burnId = burn.GetUniqueLoadID();
        }
        subject.Kill(null, null); // Explicit new corpse INPUT; not a gameplay destruction outcome.
        var corpse = subject.Corpse;
        if (corpse == null || !corpse.Spawned || corpse.InnerPawn != subject || !subject.Dead)
            throw new InvalidOperationException("Native input death did not produce the expected new loose corpse.");
        var marker = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Autopistol"));
        if (subject.inventory?.innerContainer.TryAdd(marker, false) != true)
            throw new InvalidOperationException("The new corpse subject must deeply own the unique pistol marker.");
        corpse.SetForbidden(false, false);
        return new { success = true, sourceGateId, destinationParentId, pawnId, cartId, removedReceiverPawns, removedReceiverHazards,
            receiverGateId = Id(receiver), corpseId = Id(corpse), subjectPawnId = Id(subject), markerId = Id(marker), burnId,
            cartCell = Cell(cart.Position), corpseCell = Cell(corpse.Position), loadRadius = cart.Ext.loadRadius,
            limits = "New corpse/health/inventory inputs plus removal of initial non-colony receiving-map pawns, including wildlife, and exact WaspDroneTrap hazards before any player arrival. Existing cart, player actors, terrain, jobs, buffers and site records are unchanged. Load through the actual manifest and native hauling jobs; this controlled fixture is not combat acceptance." };
    });

    [Tool("rimgate/prepare_minified_nested_corpse", Description = "Create a separate minified steel cart containing a new corpse/pawn/inventory marker and Steel17 sibling on an empty owned receiving-site cell. This is starting input, not loading/travel acceptance; preserves the original cart and actors.")]
    public static Task<object> PrepareMinified(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string sourceGateId, int destinationParentId, string pawnId, string originalCartId, string originalCorpseId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var site = ResolveSite(destinationParentId);
        var map = site.Map ?? throw new InvalidOperationException("Use the loaded owned receiving site.");
        var source = Find.Maps.SelectMany(m => m.listerThings.AllThings.OfType<Building_Gate>())
            .Single(g => Id(g) == sourceGateId);
        var pusher = source.Map.mapPawns.FreeColonistsSpawned.Single(p => Id(p) == pawnId);
        var original = map.listerThings.AllThings.OfType<Building_MobileContainer>().Single(c => Id(c) == originalCartId);
        if (source.Position != new IntVec3(110, 0, 130) || source.ConnectedDHD?.Position != new IntVec3(110, 0, 134)
            || pusher.Dead || pusher.Downed || pusher.InMentalState
            || map.mapPawns.AllPawnsSpawned.Any(p => p.Faction == Faction.OfPlayer || p.IsColonist || p.IsPrisonerOfColony || p.IsSlaveOfColony)
            || original.Faction != Faction.OfPlayer || !original.InnerContainer.OfType<Corpse>()
                .Any(c => Id(c) == originalCorpseId && c.InnerPawn?.Dead == true))
            throw new InvalidOperationException("Return the healthy operator home and retain the original physical cart/corpse on the owned receiving map before staging this separate input.");
        var gates = map.listerThings.AllThings.OfType<Building_Gate>().ToArray();
        var cell = new[] { new IntVec3(3, 0, 0), new IntVec3(-3, 0, 0), new IntVec3(0, 0, 3), new IntVec3(0, 0, -3) }
            .Select(o => original.Position + o).Where(c => c.InBounds(map) && c.Standable(map)
                && c.GetThingList(map).Count == 0 && gates.All(g => !g.VortexCells.Contains(c)))
            .DefaultIfEmpty(IntVec3.Invalid).First();
        if (!cell.IsValid) throw new InvalidOperationException("No empty standable non-vortex starting cell beside the original cart.");
        var subject = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
            canGeneratePawnRelations: false, fixedBiologicalAge: 25f, fixedChronologicalAge: 25f));
        subject.Name = new NameTriple("Rimgate", "Minified", "corpse input");
        GenSpawn.Spawn(subject, cell, map);
        var torso = subject.RaceProps.body.AllParts.First(p => p.def.defName == "Torso");
        var burn = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("Burn"), subject, torso);
        burn.Severity = 3f;
        subject.health.AddHediff(burn);
        subject.Kill(null, null);
        var corpse = subject.Corpse;
        if (corpse == null || !corpse.Spawned || corpse.InnerPawn != subject || !subject.Dead)
            throw new InvalidOperationException("Native starting death did not create the expected corpse.");
        var marker = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Autopistol"));
        if (subject.inventory?.innerContainer.TryAdd(marker, false) != true)
            throw new InvalidOperationException("The new minified subject must own its inventory marker.");
        corpse.DeSpawn();
        var cart = (Building_MobileContainer)ThingMaker.MakeThing(RimgateDefOf.Rimgate_Wheelbarrow, ThingDefOf.Steel);
        cart.SetFaction(Faction.OfPlayer);
        cart.Rotation = Rot4.West;
        var sibling = ThingMaker.MakeThing(ThingDefOf.Steel);
        sibling.stackCount = 17;
        if (!cart.InnerContainer.TryAdd(corpse, false) || !cart.InnerContainer.TryAdd(sibling, false))
            throw new InvalidOperationException("The new starting cart must deeply own its corpse and Steel17 sibling.");
        var shell = (MinifiedThing)ThingMaker.MakeThing(ThingDefOf.MinifiedThing);
        shell.InnerThing = cart;
        GenSpawn.Spawn(shell, cell, map, Rot4.East);
        cart.Rotation = Rot4.West; // Explicit distinct inner facing for this new starting input.
        return new { success = true, sourceGateId, destinationParentId, pawnId, originalCartId, originalCorpseId,
            shellId = Id(shell), cartId = Id(cart), corpseId = Id(corpse), subjectPawnId = Id(subject), markerId = Id(marker),
            siblingId = Id(sibling), siblingCount = sibling.stackCount, burnId = burn.GetUniqueLoadID(),
            shellRotation = shell.Rotation.AsInt, cartRotation = cart.Rotation.AsInt, cell = Cell(shell.Position),
            limits = "Separate preloaded minified starting input with distinct shell/inner rotations; no original cart, health, jobs, links or site snapshots altered. Does not claim manifest-loading or cart-conversion acceptance for this new root." };
    });

    [Tool("rimgate/nested_corpse_report", Description = "Read actual nested corpse/pawn/marker deep ownership across maps and world holders, plus separate site value records. Missing/malformed objects are reported without reading corpse labels or rebuilding anything.")]
    public static Task<object> Report(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string sourceGateId, int destinationParentId, string pawnId, string corpseId,
        string subjectPawnId, string markerId, string originalCartId = "")
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var site = ResolveSite(destinationParentId);
        var occurrences = new List<Occurrence>();
        var visited = new HashSet<IThingHolder>(ReferenceEqualityComparer.Instance);
        foreach (var map in Find.Maps) Walk(map, "map:" + map.uniqueID, visited, occurrences);
        foreach (var worldObject in Find.WorldObjects.AllWorldObjects)
            if (worldObject is IThingHolder holder) Walk(holder, "world:" + worldObject.GetUniqueLoadID(), visited, occurrences);
        var things = occurrences.Select(o => o.Thing).Distinct(ReferenceEqualityComparer.Instance).Cast<Thing>().ToArray();
        var corpse = things.OfType<Corpse>().SingleOrDefault(c => Id(c) == corpseId);
        var subject = things.OfType<Pawn>().SingleOrDefault(p => Id(p) == subjectPawnId);
        var marker = things.SingleOrDefault(t => Id(t) == markerId);
        var pusher = things.OfType<Pawn>().SingleOrDefault(p => Id(p) == pawnId);
        var source = things.OfType<Building_Gate>().SingleOrDefault(g => Id(g) == sourceGateId);
        var records = new List<object>();
        Flatten((List<TransitStoredThing>)StoredThings.GetValue(site) ?? [], "site", records);
        return new
        {
            success = true, tick = Find.TickManager.TicksGame, sourceGateId, destinationParentId, originalCartId,
            sourceExists = source != null, sourceActive = source?.IsActive,
            destinationHasMap = site.HasMap, destinationMapId = site.Map?.uniqueID,
            anyPawnBlockingMapRemoval = site.Map?.mapPawns.AnyPawnBlockingMapRemoval,
            siteScavenged = (bool)WasLooted.GetValue(site)!,
            corpse = Tracked(corpseId, occurrences), subject = Tracked(subjectPawnId, occurrences), marker = Tracked(markerId, occurrences),
            pusher = Tracked(pawnId, occurrences),
            pusherPawn = pusher == null ? null : new { dead = pusher.Dead, downed = pusher.Downed,
                spawned = pusher.Spawned, mapId = pusher.MapHeld?.uniqueID, job = pusher.CurJobDef?.defName,
                position = Cell(pusher.Position) },
            graph = new { innerPawnId = corpse?.InnerPawn == null ? null : Id(corpse.InnerPawn),
                malformedCorpse = corpse != null && corpse.InnerPawn == null,
                exactInnerPawn = corpse != null && subject != null && ReferenceEquals(corpse.InnerPawn, subject),
                pawnParentIsCorpse = subject != null && ReferenceEquals(subject.ParentHolder, corpse),
                dead = subject?.Dead, inventoryIds = subject?.inventory?.innerContainer.Select(Id).ToArray(),
                exactMarkerInInventory = marker != null && subject?.inventory?.innerContainer.Contains(marker) == true,
                markerParentIsInventory = marker != null && ReferenceEquals(marker.ParentHolder, subject?.inventory),
                health = subject?.health?.hediffSet?.hediffs.Select(h => new { id = h.GetUniqueLoadID(),
                    defName = h.def.defName, part = h.Part?.def.defName, severity = h.Severity }).ToArray() },
            worldPawnRegistrations = Find.WorldPawns.AllPawnsAliveOrDead.Where(p => Id(p) == subjectPawnId)
                .Select(p => new { id = Id(p), sameActualSubject = ReferenceEquals(p, subject), dead = p.Dead }).ToArray(),
            carts = things.OfType<Building_MobileContainer>().Select(c => new { id = Id(c), defName = c.def.defName,
                spawned = c.Spawned, mapId = c.MapHeld?.uniqueID, cell = Cell(c.Position), rotation = c.Rotation.AsInt, stuff = c.Stuff?.defName,
                hitPoints = c.HitPoints, heldIds = c.InnerContainer.Select(Id).ToArray(), heldCount = c.InnerContainer.Count,
                overlapsVortex = c.Spawned && c.Map.listerThings.AllThings.OfType<Building_Gate>()
                    .Any(g => g.VortexCells.Any(cell => c.OccupiedRect().Contains(cell))),
                parentHolder = c.ParentHolder?.GetType().FullName }).ToArray(),
            minifiedRoots = things.OfType<MinifiedThing>().Select(m => new { id = Id(m),
                innerThingId = m.InnerThing == null ? null : Id(m.InnerThing), rotation = m.Rotation.AsInt,
                spawned = m.Spawned, parentHolder = m.ParentHolder?.GetType().FullName }).ToArray(),
            proxies = things.OfType<Thing_MobileCartProxy>().Select(c => new { id = Id(c),
                heldIds = c.InnerContainer.Select(Id).ToArray(), parentHolder = c.ParentHolder?.GetType().FullName }).ToArray(),
            allCorpses = things.OfType<Corpse>().Select(c => new { id = Id(c), spawned = c.Spawned,
                innerPawnId = c.InnerPawn == null ? null : Id(c.InnerPawn), malformed = c.InnerPawn == null,
                parentHolder = c.ParentHolder?.GetType().FullName }).ToArray(),
            storedRecordCount = records.Count, storedRecords = records,
            limits = "Actual owner rows exclude queue/job aliases; world-pawn registrations are separate references. Value records have no original identity and are never counted as deep ownership. Cart IDs can change on native conversion. Missing/malformed remains are observations, not repaired outcomes; paused boundaries avoid sequential lifecycle races."
        };
    });

    private static object Tracked(string id, List<Occurrence> occurrences)
    {
        var hits = occurrences.Where(o => Id(o.Thing) == id).ToArray();
        return new { id, found = hits.Length != 0, ownershipOccurrenceCount = hits.Length,
            distinctObjectCount = hits.Select(o => o.Thing).Distinct(ReferenceEqualityComparer.Instance).Count(),
            locations = hits.Select(o => new { path = o.Path, parentHolder = o.Thing.ParentHolder?.GetType().FullName,
                spawned = o.Thing.Spawned, mapId = o.Thing.MapHeld?.uniqueID, destroyed = o.Thing.Destroyed }).ToArray() };
    }

    private static void Walk(IThingHolder holder, string path, HashSet<IThingHolder> visited, List<Occurrence> result)
    {
        if (!visited.Add(holder)) return;
        var here = path + "/" + (holder is Thing t ? Id(t) : holder.GetType().Name);
        var owner = holder.GetDirectlyHeldThings();
        if (owner != null) foreach (var thing in owner) result.Add(new Occurrence(thing, here));
        // A malformed generic Corpse has no inner pawn; do not ask its child walker to dereference one.
        if (holder is Corpse corpse && corpse.InnerPawn == null) return;
        var children = new List<IThingHolder>();
        holder.GetChildHolders(children);
        foreach (var child in children) Walk(child, here, visited, result);
    }

    private static void Flatten(IEnumerable<TransitStoredThing> records, string path, List<object> result)
    {
        int index = 0;
        foreach (var record in records)
        {
            string here = path + "/" + index++;
            result.Add(new { path = here, defName = record.Def?.defName, count = record.Count,
                childCount = record.Contents?.Count ?? 0, hasOriginalThingId = false });
            if (record.Contents != null) Flatten(record.Contents, here, result);
        }
    }
    private static WorldObject_GateTransitSite ResolveSite(int id) => Find.WorldObjects.MapParents
        .OfType<WorldObject_GateTransitSite>().Single(s => s.ID == id && s.SiteName == "Rimgate transit destination");
    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected private 1.7 profile and loaded development package.");
    }
    private sealed record Occurrence(Thing Thing, string Path);
    private static object Cell(IntVec3 cell) => new { x = cell.x, z = cell.z };
    private static string Id(Thing thing) => thing.GetUniqueLoadID();
}
