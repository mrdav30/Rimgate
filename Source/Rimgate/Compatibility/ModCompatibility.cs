using HarmonyLib;
using System.Linq;
using System.Reflection;
using Verse;

namespace Rimgate;

[StaticConstructorOnStartup]
public class ModCompatibility
{
    // Alpha Genes (use PackageId.Equals to match only the main Alpha Genes mod, and not the Alpha Genes - Insectoid Mutations gene pack)
    public static bool AlphaGenesIsActive => ModLister.AllInstalledMods
        .Where(x => x.Active && x.PackageId.Equals("sarg.alphagenes".ToLower()))
        .Any();

    // Applies to both DBH and DBH Lite (with Thirst module)
    public static bool DbhIsActive => ModLister.AllInstalledMods
        .Where(x => x.Active && x.PackageId.Contains("Dubwise.DubsBadHygiene".ToLower()))
        .Any();

    static ModCompatibility()
    {
        if (ModCompatibility.AlphaGenesIsActive)
        {
            if (RimgateMod.debug)
                Log.Message("Rimgate :: Alpha Genes detected!");

            // Conditionally patch BreakSomeBones patch to patients on Sarcophagus
            var targetPatchClass = AccessTools.TypeByName("AlphaGenes_Pawn_HealthTracker_MakeDowned_Patch");
            MethodInfo original = AccessTools.Method(targetPatchClass, "BreakSomeBones");
            HarmonyMethod prefix = new HarmonyMethod(
                typeof(AlphaGenesCompatibility),
                nameof(AlphaGenesCompatibility.SkipIfPawnIsOnSarcophagus));
            RimgateMod.harmony.Patch(original, prefix);
        }

        if (ModCompatibility.DbhIsActive)
        {
            if (RimgateMod.debug)
                Log.Message("Rimgate :: Dubs Bad Hygiene detected!");

            // Conditionally patch WorkGiver_washPatient to ignore MedPods
            var workGiver_washPatientType = AccessTools.TypeByName("WorkGiver_washPatient");
            MethodInfo original = AccessTools.Method(workGiver_washPatientType, "ShouldBeWashedBySomeone");
            HarmonyMethod postfix = new HarmonyMethod(
                typeof(DbhCompatibility),
                nameof(DbhCompatibility.ShouldBeWashedBySomeonePostfix));
            RimgateMod.harmony.Patch(original, postfix: postfix);
        }
    }
}
