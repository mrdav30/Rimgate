using Verse;

namespace Rimgate;

public class RimgateModSettings : ModSettings
{
    #region Clone Pod

    public static bool EnableCloneIncidents = true;
    public static bool CloneTattoos = false;
    public static bool NoSkillLoss = false;
    public static bool GenerateSocialRelations = false;
    public static bool MinorFailures = true;
    public static int MinorFailureChance = 10;
    public static bool MajorFailures = false;
    public static int MajorFailureChance = 1;

    #endregion

    public virtual void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look<bool>(ref RimgateModSettings.NoSkillLoss, "NoSkillLoss", false, true);
        Scribe_Values.Look<bool>(ref RimgateModSettings.CloneTattoos, "CloneTattoos", false, true);
        Scribe_Values.Look<bool>(ref RimgateModSettings.GenerateSocialRelations, "GenerateSocialRelations", false, true);
        Scribe_Values.Look<bool>(ref RimgateModSettings.MinorFailures, "MinorFailures", true, true);
        Scribe_Values.Look<bool>(ref RimgateModSettings.MajorFailures, "MajorFailures", false, true);
        Scribe_Values.Look<int>(ref RimgateModSettings.MinorFailureChance, "MinorFailureChance", 10, true);
        Scribe_Values.Look<int>(ref RimgateModSettings.MajorFailureChance, "MajorFailureChance", 1, true);
        Scribe_Values.Look<bool>(ref RimgateModSettings.EnableCloneIncidents, "EnableIncidents", true, true);
    }
}
