using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimgate;

public class RimgateModSettings : ModSettings
{
    #region Constants and Fields

    private enum SettingsTab
    {
        General,
        ClonePod
    }

    private const float TabTopMargin = 4f;
    private const float TabHeight = 32f;
    private const float TabContentGap = 6f;
    private const float ContentPadding = 12f;
    private const float ClonePodViewHeight = 1700f;

    private readonly List<TabRecord> _tabs = [];
    private SettingsTab _activeTab = SettingsTab.General;
    private Vector2 _clonePodScrollPosition = Vector2.zero;

    private string _baseCalibrationTicksBuffer;
    private string _baseIncubationTicksBuffer;

    #endregion

    #region Clone Pod

    public int BaseCalibrationTicks = 4500;

    public int BaseIncubationTicks = 120000; // 2 days

    public float FullCloneFactor = 1.5f;

    public float EnhancedCloneFactor = 2f;

    public float ReconstructionCloneFactor = 2.5f;

    public float StabilizerBiomassCostReduction = 0.05f;

    public float InducerCalibrationSpeedFactor = 1.1f;

    public float InducerIncubationSpeedFactor = 1.25f;

    public bool EnableCloneIncidents = true;

    public bool CloneTattoos = false;

    public bool NoSkillLoss = false;

    public bool GenerateSocialRelations = false;

    public bool MinorFailures = true;

    public int MinorFailureChance = 10;

    public bool MajorFailures = false;

    public int MajorFailureChance = 1;

    public int MedicineSkillReq = 10;

    public float StabilizerDeteriorationFactor = 0.5f;

    #endregion

    public bool EnableAsteroidIncidents = true;

    public RimgateLogLevel LogLevel = RimgateLogLevel.Warning;

    public RimgateModSettings() => ApplyBuffers();

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

        ApplyBuffers();
    }

    private void ApplyBuffers()
    {
        _baseCalibrationTicksBuffer = BaseCalibrationTicks.ToString();
        _baseIncubationTicksBuffer = BaseIncubationTicks.ToString();
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Rect tabsRect = new(inRect.x, inRect.y + TabTopMargin, inRect.width, TabHeight);
        DrawTabs(tabsRect);

        Rect contentRect = new(
            inRect.x,
            tabsRect.yMax + TabContentGap,
            inRect.width,
            inRect.height - tabsRect.height - TabTopMargin - TabContentGap);
        Widgets.DrawMenuSection(contentRect);

        Rect innerContentRect = contentRect.ContractedBy(ContentPadding);
        switch (_activeTab)
        {
            case SettingsTab.General:
                DrawGeneralTab(innerContentRect);
                break;
            default:
                DrawClonePodTab(innerContentRect);
                break;
        }

        ClampSettingsValues();
    }

    private void DrawTabs(Rect tabsRect)
    {
        _tabs.Clear();
        _tabs.Add(new TabRecord(
            "RG_Settings_Tab_General".Translate(),
            delegate { _activeTab = SettingsTab.General; },
            _activeTab == SettingsTab.General));
        _tabs.Add(new TabRecord(
            "RG_Settings_Tab_ClonePod".Translate(),
            delegate { _activeTab = SettingsTab.ClonePod; },
            _activeTab == SettingsTab.ClonePod));

        TabDrawer.DrawTabs(tabsRect, _tabs);
    }

    private void DrawGeneralTab(Rect contentRect)
    {
        Listing_Standard listing = new();
        listing.Begin(contentRect);

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_General_Label".Translate(), addTopGap: false);
        DrawUtil.DrawCheckbox(listing, "RG_Settings_EnableAsteroidIncidents_Label".Translate(), ref EnableAsteroidIncidents);

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_Logging_Label".Translate());
        DrawLogLevelSelector(listing);

        Rect defaultsButtonRect = listing.GetRect(DrawUtil.SliderHeight + 2f);
        if (Widgets.ButtonText(defaultsButtonRect, "RG_Settings_RestoreDefaults_Button".Translate()))
            RestoreDefaults();

        listing.Gap(DrawUtil.SliderGap);
        listing.End();
    }

    public void DrawLogLevelSelector(Listing_Standard listing)
    {
        Rect rect = listing.GetRect(DrawUtil.SliderHeight);
        float labelWidth = Mathf.Min(DrawUtil.SettingsLabelWidth, rect.width * 0.6f);

        Rect labelRect = new(rect.x, rect.y, labelWidth, rect.height);
        Rect buttonRect = new(rect.x + labelWidth + 6f, rect.y, rect.width - labelWidth - 6f, rect.height);

        Widgets.Label(labelRect, "RG_Settings_LogLevel_Label".Translate());
        if (Widgets.ButtonText(buttonRect, LogUtil.GetLogLevelLabel(LogLevel)))
            LogLevel = LogUtil.GetNextLogLevel(LogLevel);

        listing.Gap(DrawUtil.SliderGap);
    }

    private void DrawClonePodTab(Rect contentRect)
    {
        float viewWidth = Mathf.Max(1f, contentRect.width - 16f);
        Rect viewRect = new(0f, 0f, viewWidth, ClonePodViewHeight);

        Widgets.BeginScrollView(contentRect, ref _clonePodScrollPosition, viewRect);
        Listing_Standard listing = new();
        listing.Begin(viewRect);

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_CloneTimings_Label".Translate(), addTopGap: false);
        DrawUtil.DrawIntSliderWithInput(
            listing,
            "RG_Settings_BaseCalibrationTicks_Label".Translate(),
            ref BaseCalibrationTicks,
            ref _baseCalibrationTicksBuffer,
            300,
            30000);
        DrawUtil.DrawIntSliderWithInput(
            listing,
            "RG_Settings_BaseIncubationTicks_Label".Translate(),
            ref BaseIncubationTicks,
            ref _baseIncubationTicksBuffer,
            60000,
            360000);

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_CloneModifiers_Label".Translate());
        DrawUtil.DrawFloatSlider(
            listing,
            "RG_Settings_FullCloneFactor_Label".Translate(),
            ref FullCloneFactor,
            0.1f,
            5f,
            0.05f,
            "0.00",
            "x");
        DrawUtil.DrawFloatSlider(
            listing,
            "RG_Settings_EnhancedCloneFactor_Label".Translate(),
            ref EnhancedCloneFactor,
            0.1f,
            5f,
            0.05f,
            "0.00",
            "x");
        DrawUtil.DrawFloatSlider(
            listing,
            "RG_Settings_ReconstructionCloneFactor_Label".Translate(),
            ref ReconstructionCloneFactor,
            0.1f,
            5f,
            0.05f,
            "0.00",
            "x");
        DrawUtil.DrawPercentSlider(
            listing,
            "RG_Settings_StabilizerBiomassCostReduction_Label".Translate(),
            ref StabilizerBiomassCostReduction,
            0f,
            0.95f,
            0.01f);
        DrawUtil.DrawFloatSlider(
            listing,
            "RG_Settings_InducerCalibrationSpeedFactor_Label".Translate(),
            ref InducerCalibrationSpeedFactor,
            0.1f,
            5f,
            0.05f,
            "0.00",
            "x");
        DrawUtil.DrawFloatSlider(
            listing,
            "RG_Settings_InducerIncubationSpeedFactor_Label".Translate(),
            ref InducerIncubationSpeedFactor,
            0.1f,
            5f,
            0.05f,
            "0.00",
            "x");

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_CloneBehavior_Label".Translate());
        DrawUtil.DrawCheckbox(listing, "RG_Settings_EnableCloneIncidents_Label".Translate(), ref EnableCloneIncidents);
        DrawUtil.DrawCheckbox(listing, "RG_Settings_CloneTattoos_Label".Translate(), ref CloneTattoos);
        DrawUtil.DrawCheckbox(listing, "RG_Settings_NoSkillLoss_Label".Translate(), ref NoSkillLoss);
        DrawUtil.DrawCheckbox(listing, "RG_Settings_GenerateSocialRelations_Label".Translate(), ref GenerateSocialRelations);

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_CloneFailures_Label".Translate());
        DrawUtil.DrawCheckbox(listing, "RG_Settings_MinorFailures_Label".Translate(), ref MinorFailures);
        if (MinorFailures)
        {
            DrawUtil.DrawIntSlider(
                listing,
                "RG_Settings_MinorFailureChance_Label".Translate(),
                ref MinorFailureChance,
                0,
                100,
                "%");
        }

        DrawUtil.DrawCheckbox(listing, "RG_Settings_MajorFailures_Label".Translate(), ref MajorFailures);
        if (MajorFailures)
        {
            DrawUtil.DrawIntSlider(
                listing,
                "RG_Settings_MajorFailureChance_Label".Translate(),
                ref MajorFailureChance,
                0,
                100,
                "%");
        }

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_CloneRequirements_Label".Translate());
        DrawUtil.DrawIntSlider(
            listing,
            "RG_Settings_MedicineSkillReq_Label".Translate(),
            ref MedicineSkillReq,
            0,
            20);
        DrawUtil.DrawFloatSlider(
            listing,
            "RG_Settings_StabilizerDeteriorationFactor_Label".Translate(),
            ref StabilizerDeteriorationFactor,
            0f,
            2f,
            0.05f,
            "0.00",
            "x");

        listing.End();
        Widgets.EndScrollView();
    }

    private void ClampSettingsValues()
    {
        MinorFailureChance = Mathf.Clamp(MinorFailureChance, 0, 100);
        MajorFailureChance = Mathf.Clamp(MajorFailureChance, 0, 100);
        MedicineSkillReq = Mathf.Clamp(MedicineSkillReq, 0, 20);
        StabilizerBiomassCostReduction = Mathf.Clamp(StabilizerBiomassCostReduction, 0f, 0.95f);
        InducerCalibrationSpeedFactor = Mathf.Max(0.1f, InducerCalibrationSpeedFactor);
        InducerIncubationSpeedFactor = Mathf.Max(0.1f, InducerIncubationSpeedFactor);
    }

    private void RestoreDefaults()
    {
        CopyValuesFrom(new RimgateModSettings());
        ClampSettingsValues();
        ApplyBuffers();
    }

    private void CopyValuesFrom(RimgateModSettings defaults)
    {
        BaseCalibrationTicks = defaults.BaseCalibrationTicks;
        BaseIncubationTicks = defaults.BaseIncubationTicks;
        FullCloneFactor = defaults.FullCloneFactor;
        EnhancedCloneFactor = defaults.EnhancedCloneFactor;
        ReconstructionCloneFactor = defaults.ReconstructionCloneFactor;
        StabilizerBiomassCostReduction = defaults.StabilizerBiomassCostReduction;
        InducerCalibrationSpeedFactor = defaults.InducerCalibrationSpeedFactor;
        InducerIncubationSpeedFactor = defaults.InducerIncubationSpeedFactor;
        EnableCloneIncidents = defaults.EnableCloneIncidents;
        CloneTattoos = defaults.CloneTattoos;
        NoSkillLoss = defaults.NoSkillLoss;
        GenerateSocialRelations = defaults.GenerateSocialRelations;
        MinorFailures = defaults.MinorFailures;
        MinorFailureChance = defaults.MinorFailureChance;
        MajorFailures = defaults.MajorFailures;
        MajorFailureChance = defaults.MajorFailureChance;
        MedicineSkillReq = defaults.MedicineSkillReq;
        StabilizerDeteriorationFactor = defaults.StabilizerDeteriorationFactor;
        EnableAsteroidIncidents = defaults.EnableAsteroidIncidents;
        LogLevel = defaults.LogLevel;
    }
}
