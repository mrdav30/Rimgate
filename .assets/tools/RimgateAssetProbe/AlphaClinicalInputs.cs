using RimBridgeServer.Sdk;
using RimWorld;
using Verse;

namespace Rimgate.AssetProbe;

// Starting inputs and read-only observations for the actual destruction/downing caller.
public sealed class AlphaClinicalInputs
{
    private static readonly string[] AddictionDefs = ["AlcoholAddiction", "SmokeleafAddiction", "PsychiteAddiction"];

    [Tool("rimgate/prepare_alpha_clinical_inputs", Description = "Stage three native default-severity addictions and one loose Minigun for a fresh adult brittle-bones patient beside the private idle sarcophagus. Does not admit, equip, attack, down, damage buildings or change treatment timers.")]
    public static Task<object> Prepare(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string patientId, string shooterId, string sarcophagusId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var map = Find.CurrentMap;
        var patient = map.mapPawns.FreeColonistsSpawned.Single(p => Id(p) == patientId);
        var shooter = map.mapPawns.FreeColonistsSpawned.Single(p => Id(p) == shooterId);
        var sarc = map.listerThings.AllThings.OfType<Building_Sarcophagus>().Single(s => Id(s) == sarcophagusId);
        var defs = AddictionDefs.Select(name => DefDatabase<HediffDef>.GetNamed(name)).ToArray();
        if (!Find.TickManager.Paused || sarc.Position != new IntVec3(132, 0, 132) || sarc.PatientPawn != null || sarc.Status != SarcophagusStatus.Idle
            || !sarc.Power.PowerOn || patient == shooter || patient.Dead || patient.Downed || shooter.Dead || shooter.Downed
            || patient.DevelopmentalStage != DevelopmentalStage.Adult || shooter.equipment?.Primary != null
            || patient.genes?.GenesListForReading.Any(g => g.def.defName == "AG_BrittleBones" && g.Active) != true
            || patient.health.hediffSet.hediffs.Any(h => defs.Contains(h.def)))
            throw new InvalidOperationException("Use the fresh healthy brittle patient, empty-handed shooter and powered idle owned sarcophagus before admission.");
        var cell = new[] { IntVec3.West, IntVec3.East, IntVec3.North, IntVec3.South }
            .Select(o => shooter.Position + o).Where(c => c.InBounds(map) && c.Standable(map) && c.GetThingList(map).Count == 0)
            .DefaultIfEmpty(IntVec3.Invalid).First();
        if (!cell.IsValid) throw new InvalidOperationException("No strictly empty native-valid loose weapon input cell.");
        var inputs = defs.Select(def => patient.health.AddHediff(def)).Select(h => new { id = h.GetUniqueLoadID(),
            defName = h.def.defName, severity = h.Severity, defaultSeverity = h.def.initialSeverity }).ToArray();
        var gun = GenSpawn.Spawn(DefDatabase<ThingDef>.GetNamed("Gun_Minigun"), cell, map);
        gun.SetForbidden(false, false);
        return new { success = true, patientId, shooterId, sarcophagusId, gunId = Id(gun), gunX = cell.x, gunZ = cell.z,
            sarcX = sarc.Position.x, sarcZ = sarc.Position.z, inputs,
            limits = "Only starting health and loose equipment inputs. Addictions use native initial severities; no treatment/HP/RNG/outcome override. Real Equip, admission and AttackStatic must follow." };
    });

    [Tool("rimgate/alpha_clinical_inputs_status", Description = "Read actual clinical patient holder/health, equipped weapon, combat job target, sarcophagus HP/progress and native tick. Does not invoke the Alpha guard or change outcomes.")]
    public static Task<object> Status(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string patientId, string shooterId, string sarcophagusId)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var things = Find.Maps.SelectMany(m => m.listerThings.AllThings).ToArray();
        var sarcs = things.OfType<Building_Sarcophagus>().ToArray();
        var pawns = things.OfType<Pawn>().Concat(sarcs.Select(s => s.PatientPawn))
            .Concat(things.OfType<Corpse>().Select(c => c.InnerPawn)).Where(p => p != null)
            .Distinct(ReferenceEqualityComparer.Instance).Cast<Pawn>().ToArray();
        var patient = pawns.SingleOrDefault(p => Id(p) == patientId);
        var shooter = pawns.SingleOrDefault(p => Id(p) == shooterId);
        var sarc = sarcs.SingleOrDefault(s => Id(s) == sarcophagusId);
        return new { success = true, tick = Find.TickManager.TicksGame, patientId, shooterId, sarcophagusId,
            patientFound = patient != null, patientDead = patient?.Dead, patientDowned = patient?.Downed,
            patientHolder = patient?.ParentHolder?.GetType().FullName,
            patientHeld = patient != null && sarc != null && ReferenceEquals(patient.ParentHolder, sarc)
                && sarc.GetDirectlyHeldThings().Any(t => ReferenceEquals(t, patient)),
            brittleActive = patient?.genes?.GenesListForReading.Any(g => g.def.defName == "AG_BrittleBones" && g.Active) == true,
            patientHediffs = patient?.health.hediffSet.hediffs.Select(h => new { id = h.GetUniqueLoadID(),
                defName = h.def.defName, severity = h.Severity, part = h.Part?.def.defName }).ToArray(),
            shooterPrimaryId = shooter?.equipment?.Primary == null ? null : Id(shooter.equipment.Primary),
            shooterJob = shooter?.CurJobDef?.defName, shooterTargetId = shooter?.CurJob?.targetA.Thing?.GetUniqueLoadID(),
            shooterDead = shooter?.Dead, shooterDowned = shooter?.Downed,
            sarcExists = sarc != null, sarcHp = sarc?.HitPoints, sarcStatus = sarc?.Status.ToString(),
            diagnosisRemaining = sarc?.DiagnosingTicks, healingRemaining = sarc?.HealingTicks,
            healingTotal = sarc?.TotalHealingTicks,
            limits = "Sequential read-only observations. A missing pawn is not destruction proof; require actual passive guard evidence for suppression." };
    });

    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (!Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase)
            || !LoadedModManager.RunningModsListForReading.Any(m => m.PackageId == "sarg.alphagenes"))
            throw new InvalidOperationException("Use the expected private 1.7 Alpha Genes profile and loaded development package.");
    }
    private static string Id(Thing thing) => thing.GetUniqueLoadID();
}
