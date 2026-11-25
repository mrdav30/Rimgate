using RimBridgeServer.Sdk;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.AssetProbe;

// Private inputs and observations. Real BringToGate jobs perform pickup/admission.
public sealed class CargoKindsProbe
{
    [Tool("rimgate/prepare_cargo_kind", Description = "Stage guarded partial-stack, corpse or anesthetized-pawn cargo inputs beside the owned active source. Creates a native-valid ReplicatorLink99 or a new fixture pawn with a unique inventory marker. Never carries, admits, transfers or completes a job.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string sourceGateId, int destinationParentId, string pawnId, string mode)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        ValidateMode(mode);
        var map = Find.CurrentMap;
        var gate = map.listerThings.AllThings.OfType<Building_Gate>().Single(g => Id(g) == sourceGateId);
        var destination = Find.WorldObjects.MapParents.OfType<WorldObject_GateTransitSite>()
            .Single(p => p.ID == destinationParentId && p.SiteName == "Rimgate transit destination");
        var receiver = destination.Map?.listerThings.AllThings.OfType<Building_Gate>().SingleOrDefault();
        if (gate.Position != new IntVec3(110, 0, 130) || gate.ConnectedDHD?.Position != new IntVec3(110, 0, 134)
            || !gate.IsActive || gate.IsReceivingGate || gate.IsOpeningQueued || gate.TicksSinceOpened < 150 || gate.IsIrisActivated
            || receiver == null || !receiver.IsActive || !receiver.IsReceivingGate || receiver.IsIrisActivated
            || gate.ConnectedGate != receiver || receiver.ConnectedGate != gate
            || gate.GetDirectlyHeldThings().Count != 0 || receiver.GetDirectlyHeldThings().Count != 0)
            throw new InvalidOperationException("Use the owned mature reciprocal empty open-iris link on its source map.");
        var hauler = map.mapPawns.FreeColonistsSpawned.Single(p => Id(p) == pawnId);
        if (hauler.Dead || hauler.Downed || hauler.InMentalState || hauler.WorkTagIsDisabled(WorkTags.Hauling)
            || hauler.carryTracker?.CarriedThing != null)
            throw new InvalidOperationException("Use the healthy hauling-capable source operator with empty hands.");
        var cargoCell = new IntVec3(114, 0, 134);
        var approach = new IntVec3(116, 0, 134);
        if (!cargoCell.InBounds(map) || !cargoCell.Standable(map) || cargoCell.GetEdifice(map) != null
            || cargoCell.GetThingList(map).Any(t => t is Pawn || t.def.category == ThingCategory.Item)
            || !approach.InBounds(map) || !approach.Standable(map))
            throw new InvalidOperationException("Reload the owned active checkpoint; cargo and approach cells must be clear and standable.");
        var definition = DefDatabase<ThingDef>.GetNamed("Rimgate_ReplicatorLink");
        int capacity = hauler.carryTracker.MaxStackSpaceEver(definition);
        if (mode == "partial" && (definition.stackLimit < 99 || capacity <= 0 || capacity >= 99))
            throw new InvalidOperationException("The loaded native carry limit must naturally split the legal 99-stack; never override stats or job count.");

        // Same known ancient-guard prerequisite as other owned smoke inputs.
        var hostiles = map.listerThings.AllThings.OfType<Pawn>().Where(p => p.Spawned && !p.Dead
            && p.Faction != Faction.OfPlayer && !p.IsColonist && !p.IsPrisonerOfColony && !p.IsSlaveOfColony
            && (p.Faction?.HostileTo(Faction.OfPlayer) == true || p.HostileTo(Faction.OfPlayer))).ToArray();
        var removedHostileIds = hostiles.Select(Id).ToArray();
        foreach (var hostile in hostiles) hostile.Destroy(DestroyMode.Vanish);
        int index = 0;
        foreach (var colonist in map.mapPawns.FreeColonistsSpawned.ToArray())
        {
            colonist.jobs.StopAll();
            colonist.drafter.Drafted = false;
            colonist.pather.StopDead();
            colonist.Position = colonist == hauler ? approach : new IntVec3(119, 0, 108 + index++);
            var wait = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
            wait.expiryInterval = 60000;
            wait.playerForced = true;
            colonist.jobs.TryTakeOrderedJob(wait, JobTag.Misc);
        }
        Thing cargo;
        Pawn subject = null;
        Thing marker = null;
        Hediff anesthetic = null;
        if (mode == "partial")
        {
            cargo = ThingMaker.MakeThing(definition);
            cargo.stackCount = 99;
            GenSpawn.Spawn(cargo, cargoCell, map);
        }
        else
        {
            subject = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
                forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: false, fixedBiologicalAge: 25f, fixedChronologicalAge: 25f));
            subject.Name = new NameTriple("Rimgate", "Cargo", mode);
            GenSpawn.Spawn(subject, cargoCell, map);
            subject.jobs.StopAll();
            if (mode == "corpse")
            {
                // Explicit corpse INPUT, not a gate-destruction outcome.
                subject.Kill(null, null);
                cargo = subject.Corpse ?? throw new InvalidOperationException("The new dead subject must produce an actual corpse.");
                if (cargo is not Corpse corpse || corpse.InnerPawn != subject || !subject.Dead)
                    throw new InvalidOperationException("The corpse must retain the exact dead fixture pawn.");
            }
            else
            {
                anesthetic = subject.health.AddHediff(DefDatabase<HediffDef>.GetNamed("Anesthetic"));
                if (subject.Dead || !subject.Downed)
                    throw new InvalidOperationException("The new subject must be alive and natively downed by ordinary anesthetic input.");
                cargo = subject;
            }
            // Death may naturally drop prior inventory. Seed this nested cargo INPUT
            // after corpse creation so the acceptance case starts with an owned marker.
            marker = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Autopistol"));
            if (subject.inventory?.innerContainer.TryAdd(marker, false) != true)
                throw new InvalidOperationException("The new subject must own its unique inventory marker.");
        }
        cargo.SetForbidden(false, false);
        map.fogGrid.ClearAllFog();
        return new
        {
            success = true, mode, removedHostileIds, sourceGateId, destinationParentId, pawnId,
            receiverGateId = Id(receiver), sourceMapId = map.uniqueID, destinationMapId = receiver.Map.uniqueID,
            cargoId = Id(cargo), cargoDef = cargo.def.defName, initialCount = cargo.stackCount,
            saveCompressible = cargo.def.saveCompressible,
            nativeCarryLimit = mode == "partial" ? capacity : hauler.carryTracker.MaxStackSpaceEver(cargo.def),
            cargoX = cargoCell.x, cargoZ = cargoCell.z,
            subjectPawnId = subject == null ? "" : Id(subject), markerId = marker == null ? "" : Id(marker),
            anestheticId = anesthetic?.GetUniqueLoadID(),
            limits = "Only native-valid loose stack or new fixture subject/marker inputs and same-map approaches. Corpse creation is explicit input; anesthetic duration/defaults are unchanged. Known initial hostile non-colony guards are removed, not colony actors. Real targeting, hauling, split, gate ownership, arrival and interruption remain production outcomes. This is not combat or corpse-site-unload acceptance."
        };
    });

    [Tool("rimgate/cargo_kind_status", Description = "Observe actual carried split ID, exact source remainder/cargo counts and corpse/downed subject inventory. Reuses transit deep-owner observers and reacquires IDs after reload; never moves or repairs cargo/jobs.")]
    public static async Task<object> Status(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string sourceGateId, int destinationParentId, string pawnId, string mode, string originalCargoId,
        string carriedCargoId = "", string subjectPawnId = "", string markerId = "")
    {
        var trackedCargoId = await context.MainThread.InvokeAsync(() =>
        {
            Guard(expectedConfigPath, expectedModRoot);
            ValidateMode(mode);
            var hauler = AllThings().OfType<Pawn>().Single(p => Id(p) == pawnId);
            return !string.IsNullOrEmpty(carriedCargoId) ? carriedCargoId : hauler.carryTracker?.CarriedThing?.GetUniqueLoadID() ?? originalCargoId;
        });
        var transit = await TransitProbe.Status(context, sourceGateId, destinationParentId, pawnId, trackedCargoId);
        var remainderTransit = mode == "partial"
            ? await TransitProbe.Status(context, sourceGateId, destinationParentId, pawnId, originalCargoId) : null;
        var subjectTransit = mode != "partial"
            ? await TransitProbe.Status(context, sourceGateId, destinationParentId, subjectPawnId, markerId) : null;
        return await context.MainThread.InvokeAsync<object>(() =>
        {
            Guard(expectedConfigPath, expectedModRoot);
            var all = AllThings();
            Thing FindThing(string id) => all.SingleOrDefault(t => Id(t) == id);
            var hauler = (Pawn)FindThing(pawnId);
            var original = FindThing(originalCargoId);
            var cargo = FindThing(trackedCargoId);
            var subject = FindThing(subjectPawnId) as Pawn;
            var marker = FindThing(markerId);
            var source = all.OfType<Building_Gate>().Single(g => Id(g) == sourceGateId);
            var sourceCellCandidates = mode == "partial" ? source.Map.listerThings.AllThings
                .Where(t => t.Spawned && t.Position == new IntVec3(114, 0, 134)
                    && t.def.defName == "Rimgate_ReplicatorLink").ToArray() : [];
            var pair = new[] { original, cargo }.Where(t => t != null)
                .Distinct(ReferenceEqualityComparer.Instance).Cast<Thing>().ToArray();
            return new
            {
                success = true, tick = Find.TickManager.TicksGame, mode, trackedCargoId,
                originalRemainderId = originalCargoId, originalRemainderLookupFound = original != null,
                partialDefinitionSaveCompressible = DefDatabase<ThingDef>.GetNamed("Rimgate_ReplicatorLink").saveCompressible,
                sourceCellCandidateCount = sourceCellCandidates.Length,
                sourceCellCandidates = sourceCellCandidates.Select(ThingState).ToArray(),
                actualCarriedId = hauler.carryTracker?.CarriedThing?.GetUniqueLoadID(),
                hauler = new { id = pawnId, dead = hauler.Dead, downed = hauler.Downed, spawned = hauler.Spawned,
                    mapId = hauler.Map?.uniqueID, job = hauler.CurJobDef?.defName,
                    jobTargetA = hauler.CurJob?.targetA.Thing?.GetUniqueLoadID(), jobCount = hauler.CurJob?.count },
                cargo = ThingState(cargo), remainder = ThingState(original),
                pairDistinctObjectCount = pair.Length, pairStackCount = pair.Sum(t => t.stackCount),
                subject = subject == null ? null : new
                {
                    id = subjectPawnId, dead = subject.Dead, downed = subject.Downed, spawned = subject.Spawned,
                    mapId = subject.MapHeld?.uniqueID, corpseId = subject.Corpse?.GetUniqueLoadID(),
                    inventoryMarkerPresent = marker != null && subject.inventory?.innerContainer.Contains(marker) == true,
                    inventoryIds = subject.inventory?.innerContainer.Select(Id).ToArray(),
                    anestheticIds = subject.health.hediffSet.hediffs.Where(h => h.def.defName == "Anesthetic").Select(h => h.GetUniqueLoadID()).ToArray()
                },
                marker = ThingState(marker), transit, remainderTransit, subjectTransit,
                limits = "Sequential read-only main-thread samples: pause at boundaries. Pair totals cover only the original remainder and actual carried split IDs, not unrelated map stacks. Exact source-cell/definition candidates diagnose native compression loss; they never replace the original ID or contribute to acceptance totals. Deep ownership is separate from gate queue aliases; no unloaded-site identity claim."
            };
        });
    }

    private static object ThingState(Thing thing) => thing == null ? null : new
    {
        id = Id(thing), defName = thing.def.defName, count = thing.stackCount,
        spawned = thing.Spawned, destroyed = thing.Destroyed, mapId = thing.MapHeld?.uniqueID,
        parentHolder = thing.ParentHolder?.GetType().Name,
        cell = new { x = thing.Position.x, z = thing.Position.z },
        innerPawnId = (thing as Corpse)?.InnerPawn?.GetUniqueLoadID()
    };
    private static Thing[] AllThings()
    {
        var things = new List<Thing>();
        var visited = new HashSet<IThingHolder>(ReferenceEqualityComparer.Instance);
        void Walk(IThingHolder holder)
        {
            if (!visited.Add(holder)) return;
            if (holder.GetDirectlyHeldThings() is { } owner) things.AddRange(owner);
            var children = new List<IThingHolder>();
            holder.GetChildHolders(children);
            foreach (var child in children) Walk(child);
        }
        foreach (var map in Find.Maps) Walk(map);
        return things.Distinct(ReferenceEqualityComparer.Instance).Cast<Thing>().ToArray();
    }
    private static string Id(Thing thing) => thing.GetUniqueLoadID();
    private static void ValidateMode(string mode)
    {
        if (mode is not ("partial" or "corpse" or "downed"))
            throw new ArgumentException("Choose partial, corpse or downed.", nameof(mode));
    }
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
