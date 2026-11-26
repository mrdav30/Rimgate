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

    [Tool("rimgate/prepare_medical_edge_inputs", Description = "Add research, initial biomass/needs and optional fresh corpse inputs to an empty owned medical fixture. Does not admit, choose clone type, calibrate, heal, abort, refuel during a cycle or shorten default timers.")]
    public static Task<object> PrepareEdges(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string hostId, string doctorId, string patientId, string podId, string sarcophagusId,
        string hostMarkerId, string patientMarkerId, string injuryId, string cloneMode,
        float initialBiomass = 250f, int looseBiomass = 250,
        string patientConditionDef = "", string patientTraitDef = "",
        string patientRaceDef = "", string patientXenotypeDef = "", bool patientEbfArm = false,
        bool patientBrittleBones = false)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        if (cloneMode is not ("Genome" or "Full" or "Enhanced" or "Reconstruct"))
            throw new ArgumentException("Choose Genome, Full, Enhanced or Reconstruct.");
        var map = Find.CurrentMap;
        var roots = map.listerThings.AllThings;
        var pod = roots.OfType<Building_CloningPod>().Single(p => p.GetUniqueLoadID() == podId);
        var sarc = roots.OfType<Building_Sarcophagus>().Single(s => s.GetUniqueLoadID() == sarcophagusId);
        var host = roots.OfType<Pawn>().Single(p => p.GetUniqueLoadID() == hostId);
        var doctor = roots.OfType<Pawn>().Single(p => p.GetUniqueLoadID() == doctorId);
        var patient = roots.OfType<Pawn>().Single(p => p.GetUniqueLoadID() == patientId);
        if (pod.Position != new IntVec3(124, 0, 132) || sarc.Position != new IntVec3(132, 0, 132)
            || pod.Status != CloningStatus.Idle || pod.HasHostPawn || pod.HasPendingClone
            || sarc.PatientPawn != null || sarc.Status != SarcophagusStatus.Idle
            || new[] { host, doctor, patient }.Any(p => p.Dead || p.Downed || p.InMentalState)
            || initialBiomass < 0 || initialBiomass > pod.Refuelable.Props.fuelCapacity
            || looseBiomass < 0 || looseBiomass > 1000)
            throw new InvalidOperationException("Use the empty owned medical fixture and bounded native-valid resource inputs.");
        if (!string.IsNullOrEmpty(patientConditionDef) || !string.IsNullOrEmpty(patientTraitDef)
            || !string.IsNullOrEmpty(patientRaceDef) || !string.IsNullOrEmpty(patientXenotypeDef)
            || patientEbfArm || patientBrittleBones)
        {
            var kind = PawnKindDefOf.Colonist;
            if (!string.IsNullOrEmpty(patientRaceDef))
            {
                if (patientRaceDef == "RSPeacekeeperDefender")
                    throw new InvalidOperationException("RSPeacekeeperDefender requires its native RSPeacekeeperDefenderSpawner; generic pawn generation omits required robot initialization.");
                if (!sarc.DisallowedRaces.Contains(patientRaceDef))
                    throw new InvalidOperationException("Race inputs are limited to the actually resolved sarcophagus disallowed list.");
                var race = DefDatabase<ThingDef>.GetNamed(patientRaceDef);
                kind = DefDatabase<PawnKindDef>.AllDefs.Where(k => k.race == race).OrderBy(k => k.defName).FirstOrDefault()
                    ?? throw new InvalidOperationException("The loaded excluded race must have a real native PawnKind.");
            }
            var xenotype = string.IsNullOrEmpty(patientXenotypeDef) ? null : DefDatabase<XenotypeDef>.GetNamed(patientXenotypeDef);
            if (xenotype != null && !sarc.DisallowedXenotypes.Contains(xenotype))
                throw new InvalidOperationException("Xenotype inputs are limited to the actually resolved sarcophagus disallowed list.");
            var brittle = patientBrittleBones ? DefDatabase<GeneDef>.GetNamed("AG_BrittleBones") : null;
            var bionicArm = patientEbfArm ? DefDatabase<HediffDef>.GetNamed("BionicArm") : null;
            if (patientEbfArm && (EbfCompatibilityWrapper.makeshiftEbfEndpoint == null
                || bionicArm.comps?.Any(c => c.GetType().FullName == "EBF.Hediffs.HediffCompProperties_MaxHPAdjust") != true))
                throw new InvalidOperationException("Use the actually loaded EBF endpoint and native EPOE BionicArm max-health component; no synthetic component inputs.");
            HediffDef condition = patientConditionDef is "" or "Healthy" ? null : DefDatabase<HediffDef>.GetNamed(patientConditionDef);
            TraitDef trait = string.IsNullOrEmpty(patientTraitDef) ? null : DefDatabase<TraitDef>.GetNamed(patientTraitDef);
            if (condition != null && !(sarc.AlwaysTreatableHediffs.Contains(condition)
                || sarc.NeverTreatableHediffs.Contains(condition) || sarc.UsageBlockingHediffs.Contains(condition)
                || sarc.NonCriticalTreatableHediffs.Contains(condition)
                || condition.defName is "Burn" or "Flu" or "BadBack" or "MissingBodyPart" or "Rimgate_SymbioteImplant"))
                throw new InvalidOperationException("Seed only a vanilla representative or an actually resolved native sarcophagus policy condition.");
            if (trait != null && !sarc.AlwaysTreatableTraits.Contains(trait) && !sarc.UsageBlockingTraits.Contains(trait))
                throw new InvalidOperationException("Seed only an actually resolved native sarcophagus trait policy input.");
            var newPatient = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, Faction.OfPlayer,
                forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: false, fixedBiologicalAge: 25f, fixedChronologicalAge: 25f));
            if (xenotype != null)
            {
                if (newPatient.genes == null) throw new InvalidOperationException("The native input pawn has no gene tracker for its excluded xenotype.");
                newPatient.genes.SetXenotype(xenotype);
            }
            newPatient.Name = new NameTriple("Rimgate", "Medical", "input");
            if (newPatient.playerSettings != null) newPatient.playerSettings.medCare = MedicalCareCategory.Best;
            if (brittle != null) newPatient.genes.AddGene(brittle, false);
            var cell = sarc.InteractionCell + new IntVec3(2, 0, 1);
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetEdifice(map) != null)
                throw new InvalidOperationException("Use a clear native-valid medical input cell.");
            GenSpawn.Spawn(newPatient, cell, map);
            Stage(newPatient, cell);
            sarc.Assignable.ForceRemovePawn(patient);
            sarc.Assignable.TryAssignPawn(newPatient);
            patient = newPatient;
            patientId = patient.GetUniqueLoadID();
            if (patient.inventory != null)
                patientMarkerId = Marker(patient, ThingDefOf.Gold, 11).GetUniqueLoadID();
            else
            {
                // Nonhuman excluded races may have no inventory. This loose marker
                // remains an input; eligibility checks make no inventory-owner claim.
                var marker = ThingMaker.MakeThing(ThingDefOf.Gold);
                marker.stackCount = 11;
                var markerCell = cell + IntVec3.East;
                if (!markerCell.InBounds(map) || !markerCell.Standable(map) || markerCell.GetEdifice(map) != null)
                    throw new InvalidOperationException("The excluded native race needs a clear loose marker input cell.");
                GenSpawn.Spawn(marker, markerCell, map);
                patientMarkerId = marker.GetUniqueLoadID();
            }
            injuryId = "";
            if (condition != null)
            {
                BodyPartRecord part = condition.defName == "MissingBodyPart"
                    ? patient.RaceProps.body.AllParts.First(p => p.def.defName == "Finger")
                    : condition.defName == "Burn" ? patient.RaceProps.body.corePart
                    : condition.defaultInstallPart != null
                        ? patient.RaceProps.body.AllParts.First(p => p.def == condition.defaultInstallPart) : null;
                var input = patient.health.AddHediff(condition, part);
                if (condition.defName == "Burn") input.Severity = 6f;
                injuryId = input.GetUniqueLoadID();
            }
            if (patientEbfArm)
            {
                var arm = patient.RaceProps.body.AllParts.First(p => p.def.defName == "Arm");
                patient.health.AddHediff(bionicArm, arm);
                var bruise = patient.health.AddHediff(DefDatabase<HediffDef>.GetNamed("Bruise"), arm);
                bruise.Severity = 6f;
                injuryId = bruise.GetUniqueLoadID();
            }
            if (trait != null)
            {
                if (patient.story?.traits == null) throw new InvalidOperationException("The native input pawn has no TraitSet for the requested policy input.");
                patient.story.traits.GainTrait(new Trait(trait, 0));
            }
        }
        foreach (string research in new[] { "Rimgate_WraithCloneFull", "Rimgate_WraithCloneEnhancement", "Rimgate_WraithCloneCorpse" })
            Find.ResearchManager.FinishProject(DefDatabase<ResearchProjectDef>.GetNamed(research), false, doctor, false);
        pod.Refuelable.ConsumeFuel(pod.Refuelable.Fuel);
        if (initialBiomass > 0) pod.Refuelable.Refuel(initialBiomass);
        pod.Refuelable.allowAutoRefuel = false;
        if (host.needs.food != null) host.needs.food.CurLevelPercentage = 0.35f;
        if (patient.needs.food != null) patient.needs.food.CurLevelPercentage = 0.45f;
        foreach (var pawn in new[] { host, patient })
            foreach (var need in pawn.needs.AllNeeds.Where(n => n.def.defName is "DBHThirst" or "Bladder" or "Hygiene"))
                // DBH naturally removes BadHygiene each need interval when clean.
                // Keep only that fresh exclusion input genuinely dirty until entry.
                need.CurLevelPercentage = ReferenceEquals(pawn, patient)
                    && patientConditionDef == "BadHygiene" && need.def.defName == "Hygiene" ? 0f : 0.4f;
        var biomassDef = DefDatabase<ThingDef>.GetNamed("Rimgate_Biomass");
        var biomassInputs = new List<Thing>();
        int remaining = looseBiomass;
        int index = 0;
        while (remaining > 0)
        {
            var cell = new IntVec3(137, 0, 134 + index++);
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetEdifice(map) != null
                || cell.GetThingList(map).Any(t => t.def.category == ThingCategory.Item))
                throw new InvalidOperationException("Keep native biomass input cells clear; no existing object may be replaced.");
            var input = ThingMaker.MakeThing(biomassDef);
            input.stackCount = Math.Min(remaining, biomassDef.stackLimit);
            GenSpawn.Spawn(input, cell, map);
            input.SetForbidden(false, false);
            biomassInputs.Add(input);
            remaining -= input.stackCount;
        }
        // Default cycles span several in-game days. Supply ordinary legal food
        // inputs now; later real draft/undraft UI releases parked actors to eat.
        var mealDef = DefDatabase<ThingDef>.GetNamed("MealSurvivalPack");
        var mealCells = new CellRect(120, 129, 20, 10).Cells.Where(c => c.InBounds(map)
            && c.Standable(map) && c.GetThingList(map).Count == 0 && c != pod.InteractionCell
            && c != sarc.InteractionCell && c != pod.InteractionCell + new IntVec3(3, 0, 1)).Take(8).ToArray();
        if (mealCells.Length != 8) throw new InvalidOperationException("The fresh medical apron needs eight clear ordinary ration cells.");
        var rations = new List<Thing>();
        foreach (var cell in mealCells)
        {
            var meal = ThingMaker.MakeThing(mealDef);
            meal.stackCount = Math.Min(10, mealDef.stackLimit);
            GenSpawn.Spawn(meal, cell, map);
            meal.SetForbidden(false, false);
            map.areaManager.Home[cell] = true;
            rations.Add(meal);
        }
        string corpseId = "";
        if (cloneMode == "Reconstruct")
        {
            // This newly generated dead donor is an input, not a clone outcome.
            var donor = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
                forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: false, fixedBiologicalAge: 25f, fixedChronologicalAge: 25f));
            donor.Name = new NameTriple("Rimgate", "Reconstruction", "input");
            var donorCell = pod.InteractionCell + new IntVec3(3, 0, 1);
            if (!donorCell.InBounds(map) || !donorCell.Standable(map) || donorCell.GetEdifice(map) != null)
                throw new InvalidOperationException("The new donor needs a clear native-valid corpse input cell.");
            GenSpawn.Spawn(donor, donorCell, map);
            donor.Kill(null, null);
            var corpse = donor.Corpse ?? throw new InvalidOperationException("The new donor must produce a native corpse.");
            corpse.SetForbidden(false, false);
            host = donor;
            hostId = donor.GetUniqueLoadID();
            hostMarkerId = Marker(donor, ThingDefOf.Steel, 7).GetUniqueLoadID();
            corpseId = corpse.GetUniqueLoadID();
        }
        var seededTrait = patient.story?.traits.allTraits.FirstOrDefault(t => sarc.AlwaysTreatableTraits.Contains(t.def));
        return new
        {
            success = true, hostId, doctorId, patientId, podId, sarcophagusId, hostMarkerId, patientMarkerId, injuryId,
            patientConditionDef, patientTraitDef, patientRaceDef, patientXenotypeDef, patientEbfArm, patientBrittleBones,
            traitInputEligible = seededTrait != null, seededTraitDef = seededTrait?.def.defName, seededTraitDegree = seededTrait?.Degree,
            cloneMode, initialBiomass = pod.Refuelable.Fuel, looseBiomass,
            biomassInputs = biomassInputs.Select(t => new { id = t.GetUniqueLoadID(), count = t.stackCount,
                x = t.Position.x, z = t.Position.z }).ToArray(), corpseId,
            rations = rations.Select(t => new { id = t.GetUniqueLoadID(), count = t.stackCount,
                x = t.Position.x, z = t.Position.z, home = map.areaManager.Home[t.Position] }).ToArray(),
            rationCount = rations.Sum(t => t.stackCount),
            routinePawnIds = map.mapPawns.FreeColonistsSpawned.Where(p => !p.Dead && !p.Downed && !p.InMentalState)
                .Select(p => p.GetUniqueLoadID()).ToArray(),
            corpseX = host.Corpse?.Position.x, corpseZ = host.Corpse?.Position.z,
            doctorLabel = doctor.Label, hostFoodInput = host.needs?.food?.CurLevelPercentage,
            patientFoodInput = patient.needs.food?.CurLevelPercentage,
            initial = Snapshot(hostId, doctorId, patientId, podId, sarcophagusId, hostMarkerId, patientMarkerId, ""),
            limits = "Only idle-fixture inputs. Clone mode is selected by normal UI, not this helper. Default costs/durations/failure rules remain unchanged. Loose biomass must enter through native refueling. Reconstruct uses a fresh dead donor and post-death inventory marker; original colonists remain intact."
        };
    });

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
        var clone = PawnById(string.IsNullOrEmpty(cloneId) ? pod.PendingClonePawn?.GetUniqueLoadID() ?? "" : cloneId);
        var outcome = Field<Building_CloningPod>("_calibrationOutcome").GetValue(pod) as CalibrationOutcome;
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
        var blockingHediffs = patient?.health.hediffSet.hediffs.Where(h => sarc.UsageBlockingHediffs.Contains(h.def)).ToArray() ?? [];
        var blockingTraits = patient?.story?.traits.allTraits.Where(t => sarc.UsageBlockingTraits.Contains(t.def)).ToArray() ?? [];
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
                genomeCalibrationLabel = pod.HostPawn == null ? null : ColoredText.StripTags("RG_CloneOccupantGenes".Translate(pod.HostPawn.LabelCap).ToString()),
                fullEntryLabel = ColoredText.StripTags("RG_EnterCloningPod".Translate("RG_BeginCloneFull".Translate()).ToString()),
                enhancedEntryLabel = ColoredText.StripTags("RG_EnterCloningPod".Translate("RG_BeginCloneSoldier".Translate()).ToString()),
                calibrationLabel = pod.HostPawn == null ? null : ColoredText.StripTags((pod.CloningType switch
                {
                    CloneType.Full => "RG_CloneOccupantFull", CloneType.Enhanced => "RG_CloneOccupantSoldier",
                    CloneType.Reconstruct => "RG_CloneReconstructDead", _ => "RG_CloneOccupantGenes"
                }).Translate(pod.HostPawn.LabelCap).ToString()),
                reconstructGizmoLabel = "RG_BeginCloneCorpse".Translate().ToString().CapitalizeFirst(),
                ejectLabel = "RG_CommandCloningPodEjectLabel".Translate().ToString(),
                abortLabel = "RG_CommandCloningPodAbortIncubationLabel".Translate().ToString(),
                containsCorpse = pod.ContainsCorpse, containedThingId = pod.ContainedThing?.GetUniqueLoadID(),
                needsSnapshotTaken = (bool)Field<Building_CloningPod>("_hostNeedsSnapshotTaken").GetValue(pod),
                savedFood = (float)Field<Building_CloningPod>("_hostSavedFoodNeed").GetValue(pod),
                savedDbhThirst = (float)Field<Building_CloningPod>("_hostSavedDbhThirstNeed").GetValue(pod),
                unpoweredTicks = (int)Field<Building_CloningPod>("_incubationUnpoweredTicks").GetValue(pod),
                outcome = outcome == null ? null : new { outcome.AnyIssues, outcome.MinorIssues, outcome.MajorIssues,
                    outcome.majorCloneDies, outcome.majorHostDies, outcome.majorIdiotClone, outcome.majorInsaneClone,
                    outcome.minorGenderSwap, outcome.minorAlbino, outcome.minorBonusGenes, outcome.minorRandomTraits },
                sharedHostCloneTraitReferences = host?.story?.traits?.allTraits.Count(t => clone?.story?.traits?.allTraits
                    .Any(other => ReferenceEquals(t, other)) == true) ?? 0,
                biomassOnMap = roots.Where(t => t.def.defName == "Rimgate_Biomass").Sum(t => t.stackCount)
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
                patientMenu = Menu(sarc, patient),
                policyDenials = new
                {
                    medicalCareAllowed = MedicalUtil.HasAllowedMedicalCareCategory(patient),
                    raceBlocked = patient != null && !patient.IsValidRaceFor(sarc.DisallowedRaces),
                    xenotypeBlocked = patient != null && !patient.IsValidXenotypeFor(sarc.DisallowedXenotypes),
                    raceLabel = patient == null ? "" : DenialLabel("RG_Sarcophagus_RaceNotAllowed".Translate(patient.def.label.CapitalizeFirst())),
                    xenotypeLabel = patient?.genes?.Xenotype == null ? "" : DenialLabel("RG_Sarcophagus_RaceNotAllowed".Translate(patient.genes.Xenotype.label.CapitalizeFirst())),
                    blockingHediffCount = blockingHediffs.Length, blockingTraitCount = blockingTraits.Length,
                    blockingHediffLabels = blockingHediffs.Select(h => DenialLabel("RG_Sarcophagus_PatientWithHediffNotAllowed".Translate(h.LabelCap))).ToArray(),
                    blockingTraitLabels = blockingTraits.Select(t => DenialLabel("RG_Sarcophagus_PatientWithTraitNotAllowed".Translate(t.LabelCap))).ToArray()
                },
                abortLabel = "RG_Sarcophagus_CommandGizmoAbortTreatment_Label".Translate().ToString(),
                needsSnapshotTaken = (bool)Field<Building_Sarcophagus>("_patientNeedsSnapshotTaken").GetValue(sarc),
                savedFood = (float)Field<Building_Sarcophagus>("_patientSavedFoodNeed").GetValue(sarc),
                savedDbhThirst = (float)Field<Building_Sarcophagus>("_patientSavedDbhThirstNeed").GetValue(sarc)
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
                RimgateMod.Settings.SarcophagusMaxDiagnosisTime, RimgateMod.Settings.SarcophagusMaxPerHediffHealingTime,
                RimgateMod.Settings.FullCloneFactor, RimgateMod.Settings.EnhancedCloneFactor,
                RimgateMod.Settings.ReconstructionCloneFactor, RimgateMod.Settings.NoSkillLoss,
                RimgateMod.Settings.MajorFailureChance, RimgateMod.Settings.MinorFailureChance
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
            solarFlareActive = power?.parent.Map?.gameConditionManager.ConditionIsActive(DefDatabase<GameConditionDef>.GetNamed("SolarFlare")),
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
                raceDef = pawn.def.defName, xenotypeDef = pawn.genes?.Xenotype?.defName, bodySize = pawn.BodySize,
                jobTargetA = pawn.CurJob?.targetA.Thing?.GetUniqueLoadID(),
                medicalOperationSpeed = readMedicalSpeed ? (double?)StatExtension.GetStatValue(pawn, RimgateDefOf.MedicalOperationSpeed, true, -1) : null,
                medicine = pawn.skills?.GetSkill(SkillDefOf.Medicine).Level,
                cloneGeneration = pawn.health.hediffSet.hediffs.OfType<Hediff_Clone>().FirstOrDefault()?.CloneGeneration,
                traits = pawn.story?.traits.allTraits.Select(t => new { defName = t.def.defName, degree = t.Degree,
                    ownerId = t.pawn?.GetUniqueLoadID(), ownerIsThisPawn = ReferenceEquals(t.pawn, pawn),
                    scenForced = t.ScenForced, t.suppressedByTrait,
                    sourceGene = t.sourceGene?.def.defName,
                    sourceGeneOwned = t.sourceGene == null || pawn.genes.GenesListForReading.Contains(t.sourceGene),
                    sourceEndogene = t.sourceGene == null ? (bool?)null : pawn.genes.Endogenes.Contains(t.sourceGene),
                    suppressorGene = t.suppressedByGene?.def.defName,
                    suppressorGeneOwned = t.suppressedByGene == null || pawn.genes.GenesListForReading.Contains(t.suppressedByGene),
                    suppressorEndogene = t.suppressedByGene == null ? (bool?)null : pawn.genes.Endogenes.Contains(t.suppressedByGene) }).ToArray(),
                childhood = pawn.story?.Childhood?.defName, adulthood = pawn.story?.Adulthood?.defName,
                skills = pawn.skills?.skills.Select(s => new { defName = s.def.defName, level = s.Level,
                    passion = s.passion.ToString() }).ToArray(),
                food = pawn.needs?.food?.CurLevelPercentage, inventory = Ids(pawn.inventory?.innerContainer),
                needs = pawn.needs?.AllNeeds.Select(n => new { defName = n.def.defName, type = n.GetType().FullName,
                    level = n.CurLevel, percentage = n.CurLevelPercentage }).ToArray(),
                hediffs = pawn.health.hediffSet.hediffs.Select(h => new
                { id = h.GetUniqueLoadID(), defName = h.def.defName, severity = h.Severity, part = h.Part?.def.defName,
                    tendableNow = h.TendableNow(), visible = h.Visible,
                    nativePartMaxHealth = h.Part == null ? (float?)null : RawPartMaxHealth(h.Part, pawn),
                    wrapperPartMaxHealth = h.Part == null ? (float?)null : EbfCompatibilityWrapper.GetMaxHealth(h.Part.def, pawn, h.Part) }).ToArray(),
                genes = pawn.genes?.GenesListForReading.Select(g => new { defName = g.def.defName,
                    endogene = pawn.genes.Endogenes.Contains(g), active = g.Active }).ToArray()
            }
        };
    }
    private static object[] Menu(Thing thing, Pawn pawn) => pawn?.Spawned == true
        ? thing.GetFloatMenuOptions(pawn).Select(o => (object)new { label = ColoredText.StripTags(o.Label), disabled = o.Disabled }).ToArray() : [];
    private static string DenialLabel(string reason) => ColoredText.StripTags("UseMedicalBed".Translate() + " (" + reason + ")");
    private static float RawPartMaxHealth(BodyPartRecord part, Pawn pawn)
    {
        var endpoint = EbfCompatibilityWrapper.makeshiftEbfEndpoint;
        if (endpoint == null) return part.def.GetMaxHealth(pawn);
        // EBF's explicit vanilla-intent API owns its protocol marker; never call
        // the patched generic getter directly or suppress diagnostics ourselves.
        var raw = endpoint.DeclaringType!.GetMethod("GetRawMaxHealth", [typeof(BodyPartDef), typeof(Pawn)])
            ?? throw new InvalidOperationException("The loaded EBF endpoint lacks its explicit raw max-health API.");
        return (float)raw.Invoke(null, [part.def, pawn]);
    }
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
        if (pawn.drafter != null) pawn.drafter.Drafted = false;
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
