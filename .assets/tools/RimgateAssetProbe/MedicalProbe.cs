using System.Collections;
using System.Reflection;
using RimBridgeServer.Sdk;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.AssetProbe;

// Private input preparation and read-only observations. Production jobs run every cycle.
public sealed class MedicalProbe
{
    private static readonly FieldInfo PendingClone = Field<Building_CloningPod>("_pendingCloneContainer");
    private static readonly FieldInfo Treatment = Field<Building_Sarcophagus>("_patientTreatableHediffs");
    private static readonly FieldInfo Traits = Field<Building_Sarcophagus>("_patientTraitsToRemove");

    [Tool("rimgate/prepare_medical_fixture", Description = "Prepare guarded private cloning/sarcophagus inputs: buildings, real power network, research, fuel, staged colonists, assignment and one burn. Does not admit patients, calibrate, clone or heal.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        var gate = map.listerThings.AllThings.OfType<Building_Gate>()
            .SingleOrDefault(g => g.Position == new IntVec3(110, 0, 130));
        if (gate == null || gate.IsActive || gate.IsOpeningQueued)
            throw new InvalidOperationException("Reload the owned smoke colony with its inactive gate at (110,130).");
        var area = new CellRect(120, 116, 20, 23);
        if (!area.Cells.All(c => c.InBounds(map)) || map.listerThings.AllThings.Any(t => area.Contains(t.Position)
            && t is Building_CloningPod or Building_Sarcophagus))
            throw new InvalidOperationException("The owned fixture requires a fresh bounded medical apron.");
        var actors = map.mapPawns.FreeColonistsSpawned.Where(p => p.RaceProps.Humanlike && !p.Dead
            && !p.Downed && !p.InMentalState && p.skills != null && p.inventory != null)
            .OrderBy(p => p.thingIDNumber).Take(3).ToArray();
        if (actors.Length != 3)
            throw new InvalidOperationException("Three healthy source colonists are required: host, doctor and patient.");
        var host = actors[0];
        var doctor = actors[1];
        var patient = actors[2];
        foreach (var cell in area)
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
        // 24 kW of vanilla generation covers the two simultaneous default peak demands.
        for (int col = 0; col < 8; col++)
            for (int row = 0; row < 3; row++)
                Spawn("VanometricPowerCell", new IntVec3(122 + col * 2, 0, 117 + row * 3), map);
        for (int x = 122; x <= 138; x++)
            LayConduit(new IntVec3(x, 0, 128), map);
        for (int x = 122; x <= 138; x += 2)
            for (int z = 117; z <= 132; z++)
                if (z != 128) LayConduit(new IntVec3(x, 0, z), map);
        var pod = (Building_CloningPod)Spawn("Rimgate_WraithCloningPod", new IntVec3(124, 0, 132), map);
        var sarc = (Building_Sarcophagus)Spawn("Rimgate_GoauldSarcophagus", new IntVec3(132, 0, 132), map, ThingDefOf.Steel);
        foreach (string research in new[] { "Rimgate_WraithBasic", "Rimgate_WraithCloneGenome", "Rimgate_GoauldBasic", "Rimgate_GoauldSarcophagus" })
            Find.ResearchManager.FinishProject(DefDatabase<ResearchProjectDef>.GetNamed(research), false, doctor, false);
        pod.Refuelable.Refuel(200f);
        pod.Refuelable.allowAutoRefuel = false;
        doctor.skills.GetSkill(SkillDefOf.Medicine).Level = 20;
        Stage(host, pod.InteractionCell + new IntVec3(1, 0, 0));
        Stage(doctor, pod.InteractionCell + new IntVec3(2, 0, 0));
        Stage(patient, sarc.InteractionCell + new IntVec3(1, 0, 0));
        patient.playerSettings.medCare = MedicalCareCategory.Best;
        sarc.Assignable.TryAssignPawn(patient);
        var injury = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("Burn"), patient,
            patient.RaceProps.body.AllParts.First(p => p.def == BodyPartDefOf.Torso));
        injury.Severity = 6f;
        patient.health.AddHediff(injury);
        Trait seededTrait = null;
        bool traitAdded = false;
        var lush = DefDatabase<TraitDef>.GetNamedSilentFail("VTE_Lush");
        if (lush != null && sarc.AlwaysTreatableTraits.Contains(lush) && patient.story?.traits != null)
        {
            seededTrait = patient.story.traits.allTraits.FirstOrDefault(t => t.def == lush && t.Degree == 0);
            if (seededTrait == null)
            {
                var input = new Trait(lush, 0);
                patient.story.traits.GainTrait(input);
                if (patient.story.traits.allTraits.Contains(input))
                {
                    seededTrait = input;
                    traitAdded = true;
                }
            }
        }
        var hostMarker = Marker(host, ThingDefOf.Steel, 7);
        var patientMarker = Marker(patient, ThingDefOf.Gold, 11);
        return new
        {
            success = true, removedHostileIds, hostileCleanupCandidates,
            seededTraitDef = seededTrait?.def.defName, seededTraitDegree = seededTrait?.Degree, traitAdded,
            traitInputEligible = lush != null && sarc.AlwaysTreatableTraits.Contains(lush),
            hostId = host.GetUniqueLoadID(), doctorId = doctor.GetUniqueLoadID(),
            patientId = patient.GetUniqueLoadID(), podId = pod.GetUniqueLoadID(), sarcophagusId = sarc.GetUniqueLoadID(),
            hostMarkerId = hostMarker.GetUniqueLoadID(), patientMarkerId = patientMarker.GetUniqueLoadID(),
            injuryId = injury.GetUniqueLoadID(), initial = Snapshot(host.GetUniqueLoadID(), doctor.GetUniqueLoadID(),
                patient.GetUniqueLoadID(), pod.GetUniqueLoadID(), sarc.GetUniqueLoadID(), hostMarker.GetUniqueLoadID(), patientMarker.GetUniqueLoadID(), ""),
            limits = "Private fixture inputs only; real float-menu entry and calibration jobs must start treatment/cloning. Vanilla 24 kW power needs normal ticks to connect. Tank fuel is seeded, so biomass hauling/refueling is not covered. Default durations/failure settings are unchanged; unrelated existing pawn conditions remain. Initially hostile spawned guards are removed before unfogging; no colony pawn is removed. A Lush trait is a fixture input only when the actual loaded sarcophagus whitelist already permits it; no def rules are changed."
        };
    });

    [Tool("rimgate/medical_status", Description = "Read exact medical fixture identities, deep owners, host/pending clone/patient, queues, fuel, needs, injuries, progress and available real menu labels. Does not advance or alter cycles.")]
    public static Task<object> Status(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string hostId, string doctorId, string patientId, string podId, string sarcophagusId,
        string hostMarkerId, string patientMarkerId, string cloneId = "")
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        return Snapshot(hostId, doctorId, patientId, podId, sarcophagusId, hostMarkerId, patientMarkerId, cloneId);
    });

    private static object Snapshot(string hostId, string doctorId, string patientId, string podId,
        string sarcophagusId, string hostMarkerId, string patientMarkerId, string cloneId)
    {
        var roots = Find.Maps.SelectMany(m => m.listerThings.AllThings).ToArray();
        var pod = roots.OfType<Building_CloningPod>().Single(p => p.GetUniqueLoadID() == podId);
        var sarc = roots.OfType<Building_Sarcophagus>().Single(p => p.GetUniqueLoadID() == sarcophagusId);
        var occurrences = new List<Occurrence>();
        var holders = new HashSet<IThingHolder>(ReferenceEqualityComparer.Instance);
        var owners = new HashSet<ThingOwner>(ReferenceEqualityComparer.Instance);
        foreach (var thing in roots)
        {
            occurrences.Add(new(thing, "map:" + thing.Map.uniqueID));
            WalkThing(thing, "map:" + thing.Map.uniqueID, holders, owners, occurrences);
        }
        // The pending owner is deep-saved separately, but the pod's holder API exposes only its host owner.
        WalkOwner((ThingOwner)PendingClone.GetValue(pod)!, "pod-pending:" + podId, holders, owners, occurrences);
        Pawn PawnById(string id) => occurrences.Select(o => o.Thing).OfType<Pawn>().FirstOrDefault(p => p.GetUniqueLoadID() == id);
        var host = PawnById(hostId);
        var doctor = PawnById(doctorId);
        var patient = PawnById(patientId);
        var plants = occurrences.Select(o => o.Thing).Distinct(ReferenceEqualityComparer.Instance)
            .OfType<ThingWithComps>().Select(t => t.GetComp<CompPowerPlant>()).Where(p => p != null).ToArray();
        var fixturePlants = plants.Where(p => p.parent.MapHeld == pod.Map
            && new CellRect(120, 116, 20, 23).Contains(p.parent.PositionHeld)
            && p.parent.def.defName == "VanometricPowerCell").ToArray();
        var duplicateTransmitters = roots.OfType<ThingWithComps>()
            .SelectMany(t => t.AllComps.OfType<CompPower>().Where(c => c.Props.transmitsPower)
                .SelectMany(c => t.OccupiedRect().Select(cell => new { mapId = t.Map.uniqueID, cell, id = t.GetUniqueLoadID() })))
            .GroupBy(x => (x.mapId, x.cell)).Where(g => g.Count() > 1)
            .Select(g => new { g.Key.mapId, x = g.Key.cell.x, z = g.Key.cell.z, ids = g.Select(x => x.id).ToArray() }).ToArray();
        return new
        {
            success = true, tick = Find.TickManager.TicksGame,
            host = Tracked(hostId, occurrences), doctor = Tracked(doctorId, occurrences, readMedicalSpeed: true), patient = Tracked(patientId, occurrences),
            hostMarker = Tracked(hostMarkerId, occurrences), patientMarker = Tracked(patientMarkerId, occurrences),
            clone = Tracked(string.IsNullOrEmpty(cloneId) ? pod.PendingClonePawn?.GetUniqueLoadID() ?? "" : cloneId, occurrences),
            pod = new
            {
                id = podId, x = pod.Position.x, z = pod.Position.z, status = pod.Status.ToString(), powered = pod.Powered,
                power = PowerState(pod.Power),
                type = pod.CloningType.ToString(), hostId = pod.HostPawn?.GetUniqueLoadID(),
                pendingCloneId = pod.PendingClonePawn?.GetUniqueLoadID(), pending = Ids((ThingOwner)PendingClone.GetValue(pod)!),
                pendingCount = ((ThingOwner)PendingClone.GetValue(pod)!).Count,
                hosts = Ids(pod.GetDirectlyHeldThings()), hostCount = pod.GetDirectlyHeldThings().Count, fuel = pod.Refuelable.Fuel,
                cycleFuelConsumed = pod.FuelForCurrentCycleConsumed, calibrationRemaining = pod.RemainingCalibrationWork,
                calibrationRemainingExact = (double)pod.RemainingCalibrationWork,
                calibrationWorkUlp = (double)MathF.BitIncrement(pod.RemainingCalibrationWork) - pod.RemainingCalibrationWork,
                incubationRemaining = pod.RemainingIncubationTicks,
                cycleCostAvailable = pod.TryGetCostForCurrentCycle(out int cost), cycleCost = cost,
                hostMenu = Menu(pod, host), doctorMenu = Menu(pod, doctor),
                genomeEntryLabel = ColoredText.StripTags("RG_EnterCloningPod".Translate("RG_BeginCloneGenome".Translate()).ToString()),
                genomeCalibrationLabel = pod.HostPawn == null ? null : ColoredText.StripTags("RG_CloneOccupantGenes".Translate(pod.HostPawn.LabelCap).ToString())
            },
            sarcophagus = new
            {
                id = sarcophagusId, x = sarc.Position.x, z = sarc.Position.z, status = sarc.Status.ToString(), powered = sarc.Power.PowerOn,
                power = PowerState(sarc.Power),
                patientId = sarc.PatientPawn?.GetUniqueLoadID(), owned = Ids(sarc.GetDirectlyHeldThings()),
                ownedCount = sarc.GetDirectlyHeldThings().Count,
                diagnosisRemaining = sarc.DiagnosingTicks, healingRemaining = sarc.HealingTicks,
                healingProgress = sarc.ProgressHealingTicks, healingTotal = sarc.TotalHealingTicks,
                treatmentIds = ((IEnumerable)Treatment.GetValue(sarc)!).Cast<Hediff>().Select(h => h.GetUniqueLoadID()).ToArray(),
                treatmentCount = ((ICollection)Treatment.GetValue(sarc)!).Count,
                treatmentTraits = ((IEnumerable)Traits.GetValue(sarc)!).Cast<Trait>().Select(t => t.def.defName).ToArray(),
                treatmentTraitCount = ((ICollection)Traits.GetValue(sarc)!).Count,
                treatmentTraitKeys = ((IEnumerable)Traits.GetValue(sarc)!).Cast<Trait>().Select(t => new
                {
                    defName = t.def.defName, degree = t.Degree,
                    patientOwnsReference = sarc.PatientPawn?.story?.traits.allTraits.Any(actual => ReferenceEquals(actual, t)) == true
                }).ToArray(),
                assigned = sarc.Assignable.AssignedPawnsForReading.Select(p => p.GetUniqueLoadID()).ToArray(),
                patientMenu = Menu(sarc, patient)
            },
            powerTopology = new
            {
                fixtureGeneratorCount = fixturePlants.Length,
                fixtureGeneratorsOnPodNet = fixturePlants.Count(p => p.PowerNet != null && ReferenceEquals(p.PowerNet, pod.Power.PowerNet)),
                fixtureGeneratorsOnSarcophagusNet = fixturePlants.Count(p => p.PowerNet != null && ReferenceEquals(p.PowerNet, sarc.Power.PowerNet)),
                consumersShareNet = pod.Power.PowerNet != null && ReferenceEquals(pod.Power.PowerNet, sarc.Power.PowerNet),
                duplicateTransmitterCellCount = duplicateTransmitters.Length, duplicateTransmitters,
                generators = plants.Select(p => new
                {
                    id = p.parent.GetUniqueLoadID(), defName = p.parent.def.defName, spawned = p.parent.Spawned,
                    mapId = p.parent.MapHeld?.uniqueID, x = p.parent.PositionHeld.x, z = p.parent.PositionHeld.z,
                    on = p.PowerOn, outputWatts = p.PowerOutput, connected = p.PowerNet != null,
                    connectedToPod = p.PowerNet != null && ReferenceEquals(p.PowerNet, pod.Power.PowerNet),
                    connectedToSarcophagus = p.PowerNet != null && ReferenceEquals(p.PowerNet, sarc.Power.PowerNet)
                }).ToArray()
            },
            settings = new
            {
                RimgateMod.Settings.BaseCalibrationTicks, RimgateMod.Settings.BaseIncubationTicks,
                RimgateMod.Settings.MedicineSkillReq, RimgateMod.Settings.MinorFailures, RimgateMod.Settings.MajorFailures,
                RimgateMod.Settings.SarcophagusMaxDiagnosisTime, RimgateMod.Settings.SarcophagusMaxPerHediffHealingTime
            },
            limits = "Reference equality distinguishes actual objects; owner occurrences and map registrations are separate. Pending clones are inspected explicitly because generic holder traversal alone does not expose that owner. Compare XML deep definitions as a separate Scribe proof; a missing tracked object alone does not establish destruction."
        };
    }

    private static object PowerState(CompPowerTrader power)
    {
        var net = power?.PowerNet;
        var parts = net?.powerComps.ToArray() ?? [];
        return new
        {
            tick = Find.TickManager.TicksGame, on = power?.PowerOn, outputWatts = power?.PowerOutput,
            parentTransmitterId = power?.connectParent?.parent.GetUniqueLoadID(), connected = net != null,
            wantsOn = power != null && FlickUtility.WantsToBeOn(power.parent),
            brokenDown = power != null && power.parent.IsBrokenDown(),
            electricityDisabled = power?.parent.Map?.gameConditionManager.ElectricityDisabled(power.parent.Map),
            generationWatts = parts.Where(p => p.PowerOn && p.PowerOutput > 0f).Sum(p => p.PowerOutput),
            drawWatts = -parts.Where(p => p.PowerOn && p.PowerOutput < 0f).Sum(p => p.PowerOutput),
            currentGainWattDaysPerTick = net?.CurrentEnergyGainRate(), storedWattDays = net?.CurrentStoredEnergy(),
            parts = parts.Select(p => new { id = p.parent.GetUniqueLoadID(), defName = p.parent.def.defName,
                on = p.PowerOn, outputWatts = p.PowerOutput }).ToArray()
        };
    }

    private sealed record Occurrence(Thing Thing, string Location);
    private static void WalkThing(Thing thing, string path, HashSet<IThingHolder> holders, HashSet<ThingOwner> owners, List<Occurrence> result)
    {
        if (thing is IThingHolder holder) WalkHolder(holder, path, holders, owners, result);
        if (thing is ThingWithComps comps)
            foreach (var comp in comps.AllComps.OfType<IThingHolder>()) WalkHolder(comp, path, holders, owners, result);
    }
    private static void WalkHolder(IThingHolder holder, string path, HashSet<IThingHolder> holders, HashSet<ThingOwner> owners, List<Occurrence> result)
    {
        if (!holders.Add(holder)) return;
        WalkOwner(holder.GetDirectlyHeldThings(), path + "/" + holder.GetType().Name, holders, owners, result);
        var children = new List<IThingHolder>();
        holder.GetChildHolders(children);
        foreach (var child in children) WalkHolder(child, path + "/" + holder.GetType().Name, holders, owners, result);
    }
    private static void WalkOwner(ThingOwner owner, string path, HashSet<IThingHolder> holders, HashSet<ThingOwner> owners, List<Occurrence> result)
    {
        if (owner == null || !owners.Add(owner)) return;
        foreach (var thing in owner)
        {
            result.Add(new(thing, "owner:" + path));
            WalkThing(thing, path, holders, owners, result);
        }
    }
    private static object Tracked(string id, List<Occurrence> occurrences, bool readMedicalSpeed = false)
    {
        var hits = occurrences.Where(o => o.Thing.GetUniqueLoadID() == id).ToArray();
        var thing = hits.FirstOrDefault()?.Thing;
        var pawn = thing as Pawn;
        return new
        {
            id, distinctObjectCount = hits.Select(o => o.Thing).Distinct(ReferenceEqualityComparer.Instance).Count(),
            ownershipOccurrences = hits.Count(o => o.Location.StartsWith("owner:")),
            mapOccurrences = hits.Count(o => o.Location.StartsWith("map:")),
            locations = hits.Select(o => o.Location).ToArray(), destroyed = thing?.Destroyed, spawned = thing?.Spawned,
            parentHolder = thing?.ParentHolder?.GetType().Name, mapId = thing?.Map?.uniqueID,
            stackCount = thing?.stackCount, defName = thing?.def.defName,
            pawn = pawn == null ? null : new
            {
                dead = pawn.Dead, downed = pawn.Downed, job = pawn.CurJobDef?.defName,
                jobTargetA = pawn.CurJob?.targetA.Thing?.GetUniqueLoadID(),
                medicalOperationSpeed = readMedicalSpeed ? (double?)StatExtension.GetStatValue(pawn, RimgateDefOf.MedicalOperationSpeed, true, -1) : null,
                medicine = pawn.skills?.GetSkill(SkillDefOf.Medicine).Level,
                cloneGeneration = pawn.health.hediffSet.hediffs.OfType<Hediff_Clone>().FirstOrDefault()?.CloneGeneration,
                traits = pawn.story?.traits.allTraits.Select(t => new { defName = t.def.defName, degree = t.Degree }).ToArray(),
                food = pawn.needs?.food?.CurLevelPercentage, inventory = Ids(pawn.inventory?.innerContainer),
                needs = pawn.needs?.AllNeeds.Select(n => new { defName = n.def.defName, type = n.GetType().FullName,
                    level = n.CurLevel, percentage = n.CurLevelPercentage }).ToArray(),
                hediffs = pawn.health.hediffSet.hediffs.Select(h => new
                { id = h.GetUniqueLoadID(), defName = h.def.defName, severity = h.Severity, part = h.Part?.def.defName }).ToArray()
            }
        };
    }
    private static object[] Menu(Thing thing, Pawn pawn) => pawn?.Spawned == true
        ? thing.GetFloatMenuOptions(pawn).Select(o => (object)new { label = ColoredText.StripTags(o.Label), disabled = o.Disabled }).ToArray() : [];
    private static string[] Ids(ThingOwner owner) => owner?.Select(t => t.GetUniqueLoadID()).ToArray() ?? [];
    private static FieldInfo Field<T>(string name) => typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static void LayConduit(IntVec3 cell, Map map)
    {
        // A vanometric cell transmits over its full 1x2 footprint. A second transmitter
        // on either occupied cell corrupts the single-network-per-cell power grid.
        if (cell.GetThingList(map).OfType<ThingWithComps>()
            .Any(t => t.AllComps.OfType<CompPower>().Any(c => c.Props.transmitsPower)))
            return;
        Spawn("PowerConduit", cell, map);
    }

    private static Thing Spawn(string name, IntVec3 cell, Map map, ThingDef stuff = null)
    {
        var thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(name), stuff);
        thing.SetFaction(Faction.OfPlayer);
        return GenSpawn.Spawn(thing, cell, map, Rot4.South, WipeMode.Vanish);
    }
    private static Thing Marker(Pawn pawn, ThingDef def, int count)
    {
        var thing = ThingMaker.MakeThing(def);
        thing.stackCount = count;
        if (!pawn.inventory.innerContainer.TryAdd(thing, false)) throw new InvalidOperationException("Fixture inventory marker could not be owned.");
        return thing;
    }
    private static void Stage(Pawn pawn, IntVec3 cell)
    {
        pawn.jobs.StopAll();
        pawn.drafter.Drafted = false;
        pawn.pather.StopDead();
        pawn.Position = cell;
        var wait = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
        wait.expiryInterval = 600000;
        wait.playerForced = true;
        pawn.jobs.TryTakeOrderedJob(wait, JobTag.Misc);
    }
    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var comparison = StringComparison.OrdinalIgnoreCase;
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), comparison)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), comparison))
            throw new InvalidOperationException("Refusing fixture use outside the expected isolated 1.7 game/profile/deployment.");
    }
}
