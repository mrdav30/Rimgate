using System.Reflection;
using RimBridgeServer.Sdk;
using RimWorld;
using Verse;

namespace Rimgate.AssetProbe;

// Invoke the actual production selector without creating a pawn or changing live RNG progress.
public sealed class CloneRandomProbe
{
    private sealed record Sample(int Seed, int? Degree, string ExceptionType, string Error);

    [Tool("rimgate/clone_random_degree_report", Description = "Invoke the loaded Rimgate CloneUtility.GetRandomDegreeForTrait for 32 bounded independent seeds on an actual TraitDef, restoring Rand state in finally after each invocation. No pawn, clone, job, settings or outcome changes. Any exception or invalid returned degree makes gameplay success false.")]
    public static Task<object> Report(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string traitDefName = "DislikesMen") => context.MainThread.InvokeAsync<object>(() =>
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected isolated 1.7 profile and private deployment.");

        var traitDef = DefDatabase<TraitDef>.GetNamed(traitDefName);
        if (traitDef.degreeDatas == null || traitDef.degreeDatas.Count == 0)
            throw new InvalidOperationException("Use an actual trait definition with at least one native degree.");
        var method = typeof(Building_CloningPod).Assembly.GetType("Rimgate.CloneUtility", throwOnError: true)!
            .GetMethod("GetRandomDegreeForTrait", BindingFlags.Static | BindingFlags.NonPublic,
                binder: null, types: [typeof(TraitDef)], modifiers: null)
            ?? throw new MissingMethodException("Loaded Rimgate CloneUtility.GetRandomDegreeForTrait(TraitDef) was not found.");
        var expectedDegrees = traitDef.degreeDatas.Select(d => d.degree).ToArray();
        var samples = new List<Sample>(32);
        for (int seed = 0; seed < 32; seed++)
        {
            Rand.PushState(seed);
            try
            {
                int degree = (int)method.Invoke(null, [traitDef])!;
                samples.Add(expectedDegrees.Contains(degree)
                    ? new Sample(seed, degree, null, null)
                    : new Sample(seed, degree, "InvalidDegree", "The production selector returned a degree absent from the loaded trait definition."));
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                samples.Add(new Sample(seed, null, ex.InnerException.GetType().FullName, ex.InnerException.Message));
            }
            finally
            {
                Rand.PopState();
            }
        }
        var failures = samples.Where(s => s.Error != null).ToArray();
        return new
        {
            success = failures.Length == 0,
            tick = Find.TickManager.TicksGame,
            traitDefName = traitDef.defName,
            degreeCount = traitDef.degreeDatas.Count,
            expectedDegrees,
            method = method.ToString(),
            assembly = method.Module.Assembly.Location,
            sampleCount = samples.Count,
            failureCount = failures.Length,
            failures,
            samples,
            limits = "Actual loaded production pure-method invocation only. Every seeded Rand scope is restored in finally; no pawn, clone, trait set, native job, game progress or failure outcome is created. This is separate from live cloning-cycle acceptance."
        };
    });
}
