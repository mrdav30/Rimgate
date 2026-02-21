using Verse;

namespace Rimgate;

public class RimgateModSettings : ModSettings
{
    #region Clone Pod

    public static int BaseCalibrationTicks = 4500;

    public static int BaseIncubationTicks = 120000; // 2 days

    public static float FullCloneFactor = 1.5f;

    public static float EnhancedCloneFactor = 2f;

    public static float ReconstructionCloneFactor = 2.5f;

    public static float StabilizerBiomassCostReduction = 0.05f;

    public static float InducerCalibrationSpeedFactor = 1.1f;

    public static float InducerIncubationSpeedFactor = 1.25f;

    public static bool EnableCloneIncidents = true;

    public static bool EnableAsteroidIncidents = true;

    public static bool CloneTattoos = false;

    public static bool NoSkillLoss = false;

    public static bool GenerateSocialRelations = false;

    public static bool MinorFailures = true;

    public static int MinorFailureChance = 10;

    public static bool MajorFailures = false;

    public static int MajorFailureChance = 1;

    public static int MedicineSkillReq = 10;

    public static float StabilizerDeteriorationFactor = 0.5f;

    #endregion

    public static RimgateLogLevel LogLevel = RimgateLogLevel.Warning;

    public static RimgateModSettings Instance => LoadedModManager.GetMod<RimgateMod>().GetSettings<RimgateModSettings>();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look<int>(ref BaseCalibrationTicks, "BaseCalibrationTicks", 4500, true);
        Scribe_Values.Look<int>(ref BaseIncubationTicks, "BaseIncubationTicks", 120000, true);
        Scribe_Values.Look<float>(ref FullCloneFactor, "FullCloneFactor", 1.5f, true);
        Scribe_Values.Look<float>(ref EnhancedCloneFactor, "EnhancedCloneFactor", 2f, true);
        Scribe_Values.Look<float>(ref ReconstructionCloneFactor, "ReconstructionCloneFactor", 2.5f, true);
        Scribe_Values.Look<float>(ref StabilizerBiomassCostReduction, "StabilizerCostReduction", 0.05f, true);
        Scribe_Values.Look<float>(ref InducerCalibrationSpeedFactor, "InducerCalibrationSpeedFactor", 1.1f, true);
        Scribe_Values.Look<float>(ref InducerIncubationSpeedFactor, "InducerIncubationSpeedFactor", 1.25f, true);
        Scribe_Values.Look<bool>(ref NoSkillLoss, "NoSkillLoss", false, true);
        Scribe_Values.Look<bool>(ref CloneTattoos, "CloneTattoos", false, true);
        Scribe_Values.Look<bool>(ref GenerateSocialRelations, "GenerateSocialRelations", false, true);
        Scribe_Values.Look<bool>(ref MinorFailures, "MinorFailures", true, true);
        Scribe_Values.Look<bool>(ref MajorFailures, "MajorFailures", false, true);
        Scribe_Values.Look<int>(ref MinorFailureChance, "MinorFailureChance", 10, true);
        Scribe_Values.Look<int>(ref MajorFailureChance, "MajorFailureChance", 1, true);
        Scribe_Values.Look<bool>(ref EnableCloneIncidents, "EnableIncidents", true, true);
        Scribe_Values.Look<bool>(ref EnableAsteroidIncidents, "EnableAsteroidIncidents", true, true);
        Scribe_Values.Look<int>(ref MedicineSkillReq, "MedicineSkillReq", 10, true);
        Scribe_Values.Look<float>(ref StabilizerDeteriorationFactor, "StabilizerDeteriorationRate", 0.5f, true);

        int logLevel = (int)LogLevel;
        Scribe_Values.Look<int>(ref logLevel, "LogLevel", (int)RimgateLogLevel.Warning, true);
        if (logLevel < (int)RimgateLogLevel.Off || logLevel > (int)RimgateLogLevel.Debug)
            logLevel = (int)RimgateLogLevel.Warning;

        LogLevel = (RimgateLogLevel)logLevel;
        LogUtil.Level = LogLevel;
    }
}
