using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace Rimgate;

public class RimgateMod : Mod
{
    private const float SliderHeight = 22f;
    private const float SliderGap = 6f;
    private const float SettingsLabelWidth = 240f;

    public static Harmony harmony;

    public static RimgateLogLevel LogLevel
    {
        get => LogUtil.Level;
        set => LogUtil.Level = value;
    }

    public override string SettingsCategory() => (string)"RG_Settings_Title".Translate();

    public RimgateMod(ModContentPack pack) : base(pack)
    {
        _ = GetSettings<RimgateModSettings>();
        LogLevel = RimgateModSettings.LogLevel;

        try
        {
            harmony = new Harmony("com.Rimgate.patches");
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            LogUtil.Error(ex, "Failed to apply Harmony patches");
        }
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        _ = GetSettings<RimgateModSettings>();

        Listing_Standard listing = new();
        listing.Begin(inRect);

        listing.Label("RG_Settings_Section_CloneTimings_Label".Translate());
        listing.GapLine();
        DrawIntSlider(
            listing,
            "RG_Settings_BaseCalibrationTicks_Label".Translate(),
            ref RimgateModSettings.BaseCalibrationTicks,
            300,
            30000);
        DrawIntSlider(
            listing,
            "RG_Settings_BaseIncubationTicks_Label".Translate(),
            ref RimgateModSettings.BaseIncubationTicks,
            60000,
            360000);

        listing.Label("RG_Settings_Section_CloneModifiers_Label".Translate());
        listing.GapLine();
        DrawFloatSlider(
            listing,
            "RG_Settings_FullCloneFactor_Label".Translate(),
            ref RimgateModSettings.FullCloneFactor,
            0.1f,
            5f,
            0.05f,
            "0.00",
            "x");
        DrawFloatSlider(
            listing,
            "RG_Settings_EnhancedCloneFactor_Label".Translate(),
            ref RimgateModSettings.EnhancedCloneFactor,
            0.1f,
            5f,
            0.05f,
            "0.00",
            "x");
        DrawFloatSlider(
            listing,
            "RG_Settings_ReconstructionCloneFactor_Label".Translate(),
            ref RimgateModSettings.ReconstructionCloneFactor,
            0.1f,
            5f,
            0.05f,
            "0.00",
            "x");
        DrawPercentSlider(
            listing,
            "RG_Settings_StabilizerBiomassCostReduction_Label".Translate(),
            ref RimgateModSettings.StabilizerBiomassCostReduction,
            0f,
            0.95f,
            0.01f);
        DrawFloatSlider(
            listing,
            "RG_Settings_InducerCalibrationSpeedFactor_Label".Translate(),
            ref RimgateModSettings.InducerCalibrationSpeedFactor,
            0.1f,
            5f,
            0.05f,
            "0.00",
            "x");
        DrawFloatSlider(
            listing,
            "RG_Settings_InducerIncubationSpeedFactor_Label".Translate(),
            ref RimgateModSettings.InducerIncubationSpeedFactor,
            0.1f,
            5f,
            0.05f,
            "0.00",
            "x");

        listing.Label("RG_Settings_Section_CloneBehavior_Label".Translate());
        listing.GapLine();
        listing.CheckboxLabeled("RG_Settings_EnableCloneIncidents_Label".Translate(), ref RimgateModSettings.EnableCloneIncidents);
        listing.CheckboxLabeled("RG_Settings_EnableAsteroidIncidents_Label".Translate(), ref RimgateModSettings.EnableAsteroidIncidents);
        listing.CheckboxLabeled("RG_Settings_CloneTattoos_Label".Translate(), ref RimgateModSettings.CloneTattoos);
        listing.CheckboxLabeled("RG_Settings_NoSkillLoss_Label".Translate(), ref RimgateModSettings.NoSkillLoss);
        listing.CheckboxLabeled("RG_Settings_GenerateSocialRelations_Label".Translate(), ref RimgateModSettings.GenerateSocialRelations);

        listing.Label("RG_Settings_Section_CloneFailures_Label".Translate());
        listing.GapLine();
        listing.CheckboxLabeled("RG_Settings_MinorFailures_Label".Translate(), ref RimgateModSettings.MinorFailures);
        if (RimgateModSettings.MinorFailures)
        {
            DrawIntSlider(
                listing,
                "RG_Settings_MinorFailureChance_Label".Translate(),
                ref RimgateModSettings.MinorFailureChance,
                0,
                100,
                "%");
        }

        listing.CheckboxLabeled("RG_Settings_MajorFailures_Label".Translate(), ref RimgateModSettings.MajorFailures);
        if (RimgateModSettings.MajorFailures)
        {
            DrawIntSlider(
                listing,
                "RG_Settings_MajorFailureChance_Label".Translate(),
                ref RimgateModSettings.MajorFailureChance,
                0,
                100,
                "%");
        }

        listing.Label("RG_Settings_Section_CloneRequirements_Label".Translate());
        listing.GapLine();
        DrawIntSlider(
            listing,
            "RG_Settings_MedicineSkillReq_Label".Translate(),
            ref RimgateModSettings.MedicineSkillReq,
            0,
            20);
        DrawFloatSlider(
            listing,
            "RG_Settings_StabilizerDeteriorationFactor_Label".Translate(),
            ref RimgateModSettings.StabilizerDeteriorationFactor,
            0f,
            2f,
            0.05f,
            "0.00",
            "x");

        listing.Label("RG_Settings_Section_Logging_Label".Translate());
        listing.GapLine();
        DrawLogLevelSelector(listing);

        RimgateModSettings.MinorFailureChance = Mathf.Clamp(RimgateModSettings.MinorFailureChance, 0, 100);
        RimgateModSettings.MajorFailureChance = Mathf.Clamp(RimgateModSettings.MajorFailureChance, 0, 100);
        RimgateModSettings.MedicineSkillReq = Mathf.Clamp(RimgateModSettings.MedicineSkillReq, 0, 20);
        RimgateModSettings.StabilizerBiomassCostReduction = Mathf.Clamp(RimgateModSettings.StabilizerBiomassCostReduction, 0f, 0.95f);
        RimgateModSettings.InducerCalibrationSpeedFactor = Mathf.Max(0.1f, RimgateModSettings.InducerCalibrationSpeedFactor);
        RimgateModSettings.InducerIncubationSpeedFactor = Mathf.Max(0.1f, RimgateModSettings.InducerIncubationSpeedFactor);
        listing.End();
    }

    private static void DrawIntSlider(
        Listing_Standard listing,
        string label,
        ref int value,
        int min,
        int max,
        string valueSuffix = "")
    {
        Rect rect = listing.GetRect(SliderHeight);
        value = Mathf.RoundToInt(Widgets.HorizontalSlider(
            rect,
            value,
            min,
            max,
            true,
            $"{label}: {value}{valueSuffix}",
            min.ToString(),
            max.ToString(),
            1f));

        listing.Gap(SliderGap);
    }

    private static void DrawFloatSlider(
        Listing_Standard listing,
        string label,
        ref float value,
        float min,
        float max,
        float roundTo,
        string format,
        string valueSuffix = "")
    {
        Rect rect = listing.GetRect(SliderHeight);
        value = Widgets.HorizontalSlider(
            rect,
            value,
            min,
            max,
            true,
            $"{label}: {value.ToString(format)}{valueSuffix}",
            min.ToString(format),
            max.ToString(format),
            roundTo);

        listing.Gap(SliderGap);
    }

    private static void DrawPercentSlider(
        Listing_Standard listing,
        string label,
        ref float value,
        float min,
        float max,
        float roundTo)
    {
        int percent = Mathf.RoundToInt(value * 100f);
        int minPercent = Mathf.RoundToInt(min * 100f);
        int maxPercent = Mathf.RoundToInt(max * 100f);

        Rect rect = listing.GetRect(SliderHeight);
        value = Widgets.HorizontalSlider(
            rect,
            value,
            min,
            max,
            true,
            $"{label}: {percent}%",
            $"{minPercent}%",
            $"{maxPercent}%",
            roundTo);

        listing.Gap(SliderGap);
    }

    private static void DrawLogLevelSelector(Listing_Standard listing)
    {
        Rect rect = listing.GetRect(SliderHeight);
        float labelWidth = Mathf.Min(SettingsLabelWidth, rect.width * 0.6f);

        Rect labelRect = new(rect.x, rect.y, labelWidth, rect.height);
        Rect buttonRect = new(rect.x + labelWidth + 6f, rect.y, rect.width - labelWidth - 6f, rect.height);

        Widgets.Label(labelRect, "RG_Settings_LogLevel_Label".Translate());
        if (Widgets.ButtonText(buttonRect, GetLogLevelLabel(RimgateModSettings.LogLevel)))
        {
            RimgateModSettings.LogLevel = GetNextLogLevel(RimgateModSettings.LogLevel);
            LogLevel = RimgateModSettings.LogLevel;
        }

        listing.Gap(SliderGap);
    }

    private static RimgateLogLevel GetNextLogLevel(RimgateLogLevel current)
    {
        return current switch
        {
            RimgateLogLevel.Off => RimgateLogLevel.Error,
            RimgateLogLevel.Error => RimgateLogLevel.Warning,
            RimgateLogLevel.Warning => RimgateLogLevel.Message,
            RimgateLogLevel.Message => RimgateLogLevel.Debug,
            _ => RimgateLogLevel.Off
        };
    }

    private static string GetLogLevelLabel(RimgateLogLevel level)
    {
        return level switch
        {
            RimgateLogLevel.Off => "RG_Settings_LogLevel_Off".Translate(),
            RimgateLogLevel.Error => "RG_Settings_LogLevel_Error".Translate(),
            RimgateLogLevel.Warning => "RG_Settings_LogLevel_Warning".Translate(),
            RimgateLogLevel.Message => "RG_Settings_LogLevel_Message".Translate(),
            RimgateLogLevel.Debug => "RG_Settings_LogLevel_Debug".Translate(),
            _ => "RG_Settings_LogLevel_Warning".Translate()
        };
    }
}
