using RimBridgeServer.Sdk;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimgate.AssetProbe;

public sealed class WorkGiverProbe
{
    [Tool("rimgate/sarcophagus_work_report", Description = "Read the actual doctor/patient destination eligibility and both doctor HasJobOnThing predicates. Does not call JobOnThing, allocate/issue jobs, change assignments, admit or treat a pawn. Use real gameplay to establish available/unavailable contexts.")]
    public static Task<object> Report(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string doctorId, string patientId) => context.MainThread.InvokeAsync<object>(() =>
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || !Prefs.DevMode
            || VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected isolated 1.7 profile and private deployment.");
        var pawns = Find.Maps.SelectMany(m => m.listerThings.AllThings.OfType<Pawn>()).ToArray();
        var doctor = pawns.Single(p => p.GetUniqueLoadID() == doctorId);
        var patient = pawns.Single(p => p.GetUniqueLoadID() == patientId);
        if (!doctor.Spawned || !patient.Spawned || doctor.Map != patient.Map)
            throw new InvalidOperationException("Observe the actual spawned same-map doctor and patient.");
        var candidates = doctor.Map.listerThings.AllThings.OfType<Building_Sarcophagus>().Select(s =>
        {
            bool valid = SarcophagusUtil.IsValidSarcophagusFor(s, patient, doctor);
            return new { id = s.GetUniqueLoadID(), valid, reason = valid || !JobFailReason.HaveReason ? "" : JobFailReason.Reason.ToString(),
                powered = s.Power?.PowerOn, hasContents = s.HasAnyContents, status = s.Status.ToString(),
                patientId = s.PatientPawn?.GetUniqueLoadID(), assignedIds = s.GetAssignedPawns()?.Select(p => p.GetUniqueLoadID()).ToArray() };
        }).ToArray();
        var best = SarcophagusUtil.FindBestSarcophagus(patient, doctor);
        var predicates = new[] { "Rimgate_Doctor_RescueToSarcophagus", "Rimgate_Doctor_CarryFromBedToSarcophagus" }
            .Select(name =>
            {
                var giver = (WorkGiver_Scanner)DefDatabase<WorkGiverDef>.GetNamed(name).Worker;
                bool hasJob = giver.HasJobOnThing(doctor, patient, false);
                return new { defName = name, hasJob, shouldSkip = giver.ShouldSkip(doctor),
                    hasUsableDestination = best != null, contractMatches = !hasJob || best != null,
                    hasJobDeclaringType = giver.GetType().GetMethod(nameof(WorkGiver_Scanner.HasJobOnThing))?.DeclaringType?.FullName };
            }).ToArray();
        return new { success = predicates.All(p => p.contractMatches), tick = Find.TickManager.TicksGame, doctorId, patientId,
            doctorJob = doctor.CurJobDef?.defName, patientJob = patient.CurJobDef?.defName,
            patientDowned = patient.Downed, patientInBed = patient.InBed(), bestDestinationId = best?.GetUniqueLoadID(), candidates, predicates,
            limits = "Read-only actual predicates/destination selection; no JobOnThing or job allocation. An available destination alone does not bypass doctor target guards. Native work scanning and real rescue/carry jobs remain gameplay acceptance." };
    });
}
