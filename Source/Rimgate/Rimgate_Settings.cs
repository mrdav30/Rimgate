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

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look<int>(ref RimgateModSettings.BaseCalibrationTicks, "BaseCalibrationTicks", 4500, true);
        Scribe_Values.Look<int>(ref RimgateModSettings.BaseIncubationTicks, "BaseIncubationTicks", 120000, true);
        Scribe_Values.Look<float>(ref RimgateModSettings.FullCloneFactor, "FullCloneFactor", 1.5f, true);
        Scribe_Values.Look<float>(ref RimgateModSettings.EnhancedCloneFactor, "EnhancedCloneFactor", 2f, true);
        Scribe_Values.Look<float>(ref RimgateModSettings.ReconstructionCloneFactor, "ReconstructionCloneFactor", 2.5f, true);
        Scribe_Values.Look<float>(ref RimgateModSettings.StabilizerBiomassCostReduction, "StabilizerCostReduction", 0.05f, true);
        Scribe_Values.Look<float>(ref RimgateModSettings.InducerCalibrationSpeedFactor, "InducerCalibrationSpeedFactor", 1.1f, true);
        Scribe_Values.Look<float>(ref RimgateModSettings.InducerIncubationSpeedFactor, "InducerIncubationSpeedFactor", 1.25f, true);
        Scribe_Values.Look<bool>(ref RimgateModSettings.NoSkillLoss, "NoSkillLoss", false, true);
        Scribe_Values.Look<bool>(ref RimgateModSettings.CloneTattoos, "CloneTattoos", false, true);
        Scribe_Values.Look<bool>(ref RimgateModSettings.GenerateSocialRelations, "GenerateSocialRelations", false, true);
        Scribe_Values.Look<bool>(ref RimgateModSettings.MinorFailures, "MinorFailures", true, true);
        Scribe_Values.Look<bool>(ref RimgateModSettings.MajorFailures, "MajorFailures", false, true);
        Scribe_Values.Look<int>(ref RimgateModSettings.MinorFailureChance, "MinorFailureChance", 10, true);
        Scribe_Values.Look<int>(ref RimgateModSettings.MajorFailureChance, "MajorFailureChance", 1, true);
        Scribe_Values.Look<bool>(ref RimgateModSettings.EnableCloneIncidents, "EnableIncidents", true, true);
        Scribe_Values.Look<int>(ref RimgateModSettings.MedicineSkillReq, "MedicineSkillReq", 10, true);
        Scribe_Values.Look<float>(ref RimgateModSettings.StabilizerDeteriorationFactor, "StabilizerDeteriorationRate", 0.5f, true);
    }
}
