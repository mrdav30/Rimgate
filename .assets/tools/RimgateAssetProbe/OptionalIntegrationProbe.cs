using System.Collections;
using System.Reflection;
using RimBridgeServer.Sdk;
using RimWorld;
using Verse;

namespace Rimgate.AssetProbe;

// Observe resolved content and registered hooks; never insert a condition or run treatment.
public sealed class OptionalIntegrationProbe
{
    [Tool("rimgate/prepare_medical_facility_inputs", Description = "Place only a native operating table and adjacent Rimgate surgical light in empty cells of the fresh owned medical apron. Uses its existing power network. Does not link facilities, change power/flick state or set stats; allow ordinary ticks and real flick jobs.")]
    public static Task<object> PrepareFacility(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string podId, string sarcophagusId) => context.MainThread.InvokeAsync<object>(() =>
    {
        var map = FacilityGuard(expectedConfigPath, expectedModRoot);
        var pod = map.listerThings.AllThings.OfType<Building_CloningPod>().Single(p => p.GetUniqueLoadID() == podId);
        var sarc = map.listerThings.AllThings.OfType<Building_Sarcophagus>().Single(p => p.GetUniqueLoadID() == sarcophagusId);
        if (pod.Position != new IntVec3(124, 0, 132) || sarc.Position != new IntVec3(132, 0, 132)
            || pod.Status != CloningStatus.Idle || pod.HasHostPawn || pod.HasPendingClone
            || sarc.Status != SarcophagusStatus.Idle || sarc.PatientPawn != null
            || !sarc.Power.PowerOn)
            throw new InvalidOperationException("Use the fresh empty owned medical fixture after native power connection.");
        var tableDef = DefDatabase<ThingDef>.GetNamed("Bed_OperatingTable");
        var lightDef = DefDatabase<ThingDef>.GetNamed("Rimgate_TauriSurgeryLights");
        if (!tableDef.GetCompProperties<CompProperties_AffectedByFacilities>().linkableFacilities.Contains(lightDef))
            throw new InvalidOperationException("The loaded native operating table must resolve the Rimgate surgical-light integration.");
        var tableCell = new IntVec3(134, 0, 135);
        var lightCell = new IntVec3(132, 0, 135);
        var tableCells = GenAdj.OccupiedRect(tableCell, Rot4.North, tableDef.size).Cells.ToArray();
        var lightCells = GenAdj.OccupiedRect(lightCell, Rot4.North, lightDef.size).Cells.ToArray();
        var allCells = tableCells.Concat(lightCells).ToArray();
        if (allCells.Distinct().Count() != allCells.Length || allCells.Any(c => !c.InBounds(map)
            || !c.Standable(map) || c.GetThingList(map).Count != 0))
            throw new InvalidOperationException("All native table/light footprint cells must be empty; no original object may be wiped or moved.");
        // Native automatic power connection reaches the existing apron conduits.
        // No extra transmitter is placed under either building or generator.
        var table = (ThingWithComps)ThingMaker.MakeThing(tableDef, tableDef.MadeFromStuff ? ThingDefOf.Steel : null);
        var light = (ThingWithComps)ThingMaker.MakeThing(lightDef, lightDef.MadeFromStuff ? ThingDefOf.Steel : null);
        table.SetFaction(Faction.OfPlayer);
        light.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(table, tableCell, map, Rot4.North);
        GenSpawn.Spawn(light, lightCell, map, Rot4.North);
        return new { success = true, tableId = table.GetUniqueLoadID(), lightId = light.GetUniqueLoadID(),
            tableX = tableCell.x, tableZ = tableCell.z, lightX = lightCell.x, lightZ = lightCell.z,
            footprint = allCells.Select(c => new { x = c.x, z = c.z }).ToArray(),
            status = FacilitySnapshot(table, light),
            limits = "Native building inputs only, on strictly empty footprints. Ordinary spawn/ticks own linking and power connection. Compare on/off/on observations after real player flick designations/jobs; no surgery outcome is implied." };
    });

    [Tool("rimgate/medical_facility_report", Description = "Read the exact private operating-table/surgical-light identities, reciprocal native facility links, active state, SurgerySuccessChanceFactor offset/value and light power/flick state. Does not refresh links, set stats or alter power.")]
    public static Task<object> FacilityReport(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string tableId, string lightId) => context.MainThread.InvokeAsync<object>(() =>
    {
        var map = FacilityGuard(expectedConfigPath, expectedModRoot);
        var table = map.listerThings.AllThings.OfType<ThingWithComps>()
            .Single(t => t.GetUniqueLoadID() == tableId && t.def.defName == "Bed_OperatingTable");
        var light = map.listerThings.AllThings.OfType<ThingWithComps>()
            .Single(t => t.GetUniqueLoadID() == lightId && t.def.defName == "Rimgate_TauriSurgeryLights");
        if (table.Position != new IntVec3(134, 0, 135) || light.Position != new IntVec3(132, 0, 135))
            throw new InvalidOperationException("Observe the exact staged native facility positions.");
        return FacilitySnapshot(table, light);
    });

    private static object FacilitySnapshot(ThingWithComps table, ThingWithComps light)
    {
        var affected = table.GetComp<CompAffectedByFacilities>();
        var facility = light.GetComp<CompFacility>();
        var power = light.GetComp<CompPowerTrader>();
        var flick = light.GetComp<CompFlickable>();
        var stat = StatDefOf.SurgerySuccessChanceFactor;
        return new { success = true, tick = Find.TickManager.TicksGame, tableId = table.GetUniqueLoadID(),
            lightId = light.GetUniqueLoadID(), mapId = table.Map.uniqueID,
            tableSpawned = table.Spawned, lightSpawned = light.Spawned,
            tableX = table.Position.x, tableZ = table.Position.z, lightX = light.Position.x, lightZ = light.Position.z,
            linkedLightIds = affected.LinkedFacilitiesListForReading.Select(t => t.GetUniqueLoadID()).ToArray(),
            linkedTableIds = facility.LinkedBuildings.Select(t => t.GetUniqueLoadID()).ToArray(),
            tableLinksLight = affected.LinkedFacilitiesListForReading.Any(t => ReferenceEquals(t, light)),
            lightLinksTable = facility.LinkedBuildings.Any(t => ReferenceEquals(t, table)),
            facilityCanBeActive = facility.CanBeActive, facilityActiveForTable = affected.IsFacilityActive(light),
            facilitySurgeryOffset = affected.GetStatOffset(stat),
            surgerySuccessChanceFactor = StatExtension.GetStatValue(table, stat, true, 0),
            authoredLightSurgeryOffset = facility.StatOffsets.FirstOrDefault(s => s.stat == stat)?.value,
            powerOn = power.PowerOn, connected = power.PowerNet != null,
            powerParentId = power.connectParent?.parent.GetUniqueLoadID(), powerOutput = power.PowerOutput,
            switchOn = flick.SwitchIsOn, wantsFlick = flick.WantsFlick(),
            limits = "Actual linked/active facility state and resulting native stat only. Structural links may remain while power is off. Real flick jobs must establish on/off/on comparisons; no stat, power, link or surgery outcome is set by this observer." };
    }

    private static Map FacilityGuard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || !Prefs.DevMode
            || VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase)
            || !LoadedModManager.RunningModsListForReading.Any(m => m.PackageId == "vanillaexpanded.vfemedical")
            || !Find.CurrentMap.listerThings.AllThings.OfType<Building_Gate>().Any(g => g.Position == new IntVec3(110, 0, 130)))
            throw new InvalidOperationException("Use the expected isolated 1.7 profile, native Medical Module and owned smoke map.");
        return Find.CurrentMap;
    }

    private sealed record Contract(string Package, string ContractName, bool Active, bool Matches, object Observed);
    private static readonly string[] Packages = ["hlx.ultratechalteredcarbon", "dubwise.dubsbadhygiene",
        "dubwise.rimatomics", "vat.epoeforked", "ogliss.thewhitecrayon.quarry", "dawnsglow.quarry",
        "zal.rimsecsecurity", "oskarpotocki.vfe.pirates", "vanillaexpanded.vfemedical",
        "vanillaracesexpanded.android", "vanillaracesexpanded.saurid", "vanillaexpanded.vanillatraitsexpanded",
        "sarg.alphagenes", "v1024.ebframework"];

    [Tool("rimgate/optional_integration_report", Description = "Read the expected private profile's actual optional packages, resolved XML contracts and Rimgate Harmony targets. expectedActivePackagesCsv must enumerate the intended optional package IDs (empty for none). No inputs or gameplay state are changed; registration/content checks alone do not prove treatment behavior.")]
    public static Task<object> Report(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string expectedActivePackagesCsv, string patientId = "") => context.MainThread.InvokeAsync<object>(() =>
    {
        var mods = LoadedModManager.RunningModsListForReading.ToArray();
        var rimgate = mods.Single(m => m.PackageId.Equals("mrdav30.rimgate", StringComparison.OrdinalIgnoreCase));
        if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(rimgate.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected isolated 1.7 game/profile/deployment.");
        bool Known(string id) => Packages.Contains(id.ToLowerInvariant()) || id.StartsWith("dubwise.dubsbadhygiene", StringComparison.OrdinalIgnoreCase);
        bool Active(string id) => mods.Any(m => m.PackageId.Equals(id, StringComparison.OrdinalIgnoreCase));
        var loaded = mods.Where(m => Known(m.PackageId)).ToArray();
        var expected = expectedActivePackagesCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.ToLowerInvariant()).Distinct().Order().ToArray();
        if (expected.Any(id => !Known(id))) throw new InvalidOperationException("Expected list contains an unknown optional integration.");
        var actual = loaded.Select(m => m.PackageId.ToLowerInvariant()).Order().ToArray();
        var ext = DefDatabase<ThingDef>.GetNamed("Rimgate_GoauldSarcophagus").GetModExtension<Building_Sarcophagus_Ext>();
        var contracts = new List<Contract>();
        void Entries(string package, string name, IEnumerable<string> observed, params string[] targets)
        {
            var values = observed?.ToArray() ?? [];
            bool active = package == "dubwise.dubsbadhygiene" ? loaded.Any(m => m.PackageId.StartsWith(package, StringComparison.OrdinalIgnoreCase)) : Active(package);
            var unresolvedRaces = name == "excludedRaces" && active
                ? targets.Where(t => DefDatabase<ThingDef>.GetNamedSilentFail(t) == null).ToArray() : [];
            contracts.Add(new(package, name, active, unresolvedRaces.Length == 0
                && targets.All(t => values.Count(v => v == t) == (active ? 1 : 0)),
                new { targets, matched = values.Where(targets.Contains).ToArray(), unresolvedRaces }));
        }
        Entries("hlx.ultratechalteredcarbon", "blockingHediffs", ext.usageBlockingHediffs?.Select(d => d.defName), "AC_EmptySleeve");
        Entries("dubwise.dubsbadhygiene", "excludedHediffs", ext.neverTreatableHediffs?.Select(d => d.defName), "BadHygiene");
        Entries("dubwise.rimatomics", "treatableHediffs", ext.alwaysTreatableHediffs?.Select(d => d.defName), "RadiationIncurable");
        Entries("zal.rimsecsecurity", "excludedRaces", ext.disallowedRaces,
            "RSPeacekeeperDefender", "RSPeacekeeperDefenderDesert", "RSPeacekeeperDefenderForest", "RSPeacekeeperDefenderWinter",
            "RSPeacekeeperEnforcer", "RSPeacekeeperEnforcerDesert", "RSPeacekeeperEnforcerForest", "RSPeacekeeperEnforcerWinter",
            "RSPeacekeeperSentinel", "RSPeacekeeperSentinelDesert", "RSPeacekeeperSentinelForest", "RSPeacekeeperSentinelWinter");
        Entries("oskarpotocki.vfe.pirates", "blockingTraits", ext.usageBlockingTraits?.Select(d => d.defName), "VFEP_WarcasketTrait");
        Entries("oskarpotocki.vfe.pirates", "treatableTraits", ext.alwaysTreatableTraits?.Select(d => d.defName), "VFEP_Shellcasket");
        Entries("vanillaracesexpanded.android", "excludedXenotypes", ext.disallowedXenotypes?.Select(d => d.defName), "VREA_AndroidAwakened", "VREA_AndroidBasic");
        Entries("vanillaracesexpanded.saurid", "excludedHediffs", ext.neverTreatableHediffs?.Select(d => d.defName),
            "VRESaurids_EggFatigue", "VRESaurids_HyperthermicSlowdown", "VRESaurids_HypothermicSlowdown");
        Entries("vanillaexpanded.vanillatraitsexpanded", "treatableTraits", ext.alwaysTreatableTraits?.Select(d => d.defName), "VTE_Lush", "VTE_Stoner");
        var bed = DefDatabase<ThingDef>.GetNamedSilentFail("Bed_OperatingTable");
        Entries("vanillaexpanded.vfemedical", "operatingTableFacilities", bed?.GetCompProperties<CompProperties_AffectedByFacilities>()?.linkableFacilities?.Select(d => d.defName), "Rimgate_TauriSurgeryLights");
        var epoe = new[] { "EPOE_OrganicOptimized", "EPOE_InstinctOptimizedEyes", "EPOE_InstinctOptimizedHand", "EPOE_InstinctOptimizedFoot" }
            .Select(name => new { defName = name, caps = DefDatabase<HediffDef>.GetNamedSilentFail(name)?.stages?
                .Where(s => s.untranslatedLabel == "Fresh" || s.label == "Fresh").SelectMany(s => s.capMods ?? [])
                .Where(c => c.capacity == PawnCapacityDefOf.Consciousness).Select(c => c.setMax).ToArray() ?? [] }).ToArray();
        bool epoeActive = Active("vat.epoeforked");
        contracts.Add(new("vat.epoeforked", "freshConsciousnessCaps", epoeActive,
            epoe.All(e => epoeActive ? e.caps.Length == 1 && e.caps[0] == 0.3f : e.caps.Length == 0), epoe));
        foreach (var quarry in new[] { "ogliss.thewhitecrayon.quarry", "dawnsglow.quarry" })
        {
            bool active = Active(quarry);
            var work = new[] { DefDatabase<ThingDef>.GetNamed("Rimgate_Malp"), DefDatabase<ThingDef>.GetNamed("Rimgate_Unas") }
                .Select(d => new { defName = d.defName, hasQuarryWork = d.race.mechEnabledWorkTypes?.Any(w => w.defName == "QuarryMining") == true }).ToArray();
            contracts.Add(new(quarry, "quarryWork", active, work.All(w => w.hasQuarryWork == loaded.Any(m => m.Name == "Quarry")), work));
        }
        Type TypeByName(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);
        var harmony = typeof(RimgateMod).GetField("harmony")!.GetValue(null) ?? throw new InvalidOperationException("Rimgate Harmony missing.");
        var harmonyType = harmony.GetType();
        bool Patched(MethodInfo target, string methodName)
        {
            if (target == null) return false;
            var info = harmonyType.GetMethod("GetPatchInfo", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [target]);
            return info != null && new[] { "Prefixes", "Postfixes" }.Any(kind =>
                ((IEnumerable)info.GetType().GetField(kind)!.GetValue(info)!).Cast<object>().Any(p =>
                    (string)p.GetType().GetField("owner")!.GetValue(p)! == "com.Rimgate.patches"
                    && ((MethodInfo)p.GetType().GetProperty("PatchMethod")!.GetValue(p)!).Name == methodName));
        }
        foreach (var spec in new[] {
            (Package: "sarg.alphagenes", Type: "AlphaGenes.AlphaGenes_Pawn_HealthTracker_MakeDowned_Patch", Method: "BreakSomeBones", Patch: "SkipIfPawnIsOnSarcophagus"),
            (Package: "dubwise.dubsbadhygiene", Type: "DubsBadHygiene.WorkGiver_washPatient", Method: "ShouldBeWashedBySomeone", Patch: "ShouldBeWashedBySomeonePostfix") })
        {
            bool active = spec.Package == "dubwise.dubsbadhygiene" ? loaded.Any(m => m.PackageId.StartsWith(spec.Package, StringComparison.OrdinalIgnoreCase)) : Active(spec.Package);
            var type = TypeByName(spec.Type);
            var target = type?.GetMethod(spec.Method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance, [typeof(Pawn)]);
            bool patched = Patched(target, spec.Patch);
            contracts.Add(new(spec.Package, "harmony:" + spec.Method, active, patched == active,
                new { target = target?.ToString(), type = type?.FullName, assembly = type?.Assembly.Location, patched }));
        }
        var ebf = TypeByName("EBF.VanillaExtender")?.GetMethod("GetMaxHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            [typeof(BodyPartDef), typeof(Pawn), typeof(BodyPartRecord)]);
        var wrapperEndpoint = typeof(EbfCompatibilityWrapper).GetField("makeshiftEbfEndpoint")!.GetValue(null) as MethodInfo;
        contracts.Add(new("v1024.ebframework", "bodyPartHealthEndpoint", Active("v1024.ebframework"),
            (ebf != null) == Active("v1024.ebframework") && Equals(ebf, wrapperEndpoint),
            new { target = ebf?.ToString(), assembly = ebf?.DeclaringType?.Assembly.Location, wrapperTarget = wrapperEndpoint?.ToString() }));
        var pawns = Find.Maps.SelectMany(m => m.listerThings.AllThings.OfType<Pawn>())
            .Concat(Find.Maps.SelectMany(m => m.listerThings.AllThings.OfType<Building_Sarcophagus>()).Select(s => s.PatientPawn)).Where(p => p != null);
        var patient = string.IsNullOrEmpty(patientId) ? null : pawns.FirstOrDefault(p => p.GetUniqueLoadID() == patientId)
            ?? throw new InvalidOperationException("Requested patient is not spawned or held by a loaded sarcophagus.");
        return new { success = expected.SequenceEqual(actual) && contracts.All(c => c.Matches), tick = Find.TickManager.TicksGame,
            expected, actual, expectedPackagesMatch = expected.SequenceEqual(actual), contractCount = contracts.Count, contracts,
            packages = loaded.Select(m => new { id = m.PackageId, m.Name, m.RootDir,
                assemblies = m.assemblies.loadedAssemblies.Select(a => new { name = a.GetName().Name,
                    version = a.GetName().Version?.ToString(), path = a.Location }).ToArray(),
                declaredVersions = m.ModMetaData.SupportedVersionsReadOnly.Select(v => v.ToString()).ToArray(),
                declares17 = m.ModMetaData.SupportedVersionsReadOnly.Any(v => v.Major == 1 && v.Minor == 7) }).ToArray(),
            patient = patient == null ? null : new { id = patient.GetUniqueLoadID(), race = patient.def.defName,
                xenotype = patient.genes?.Xenotype?.defName, allowedRace = patient.IsValidRaceFor(ext.disallowedRaces),
                allowedXenotype = patient.IsValidXenotypeFor(ext.disallowedXenotypes),
                blockingHediffs = patient.health.hediffSet.hediffs.Where(h => ext.usageBlockingHediffs.Contains(h.def)).Select(h => h.def.defName).ToArray(),
                blockingTraits = patient.story?.traits.allTraits.Where(t => ext.usageBlockingTraits.Contains(t.def)).Select(t => t.def.defName).ToArray() },
            limits = "Resolved XML and actual hook registration only. Treatment, eligibility UI, facilities, EBF health behavior and optional-mod runtime compatibility require their real gameplay checks. Quarry installed metadata supports only through 1.6; presence is not acceptance." };
    });
}
