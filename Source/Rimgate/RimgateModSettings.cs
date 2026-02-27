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
        Gate,
        ClonePod,
        Sarcophagus
    }

    private const float ContentPadding = 12f;
    private const float ClonePodBottomPadding = 4f;
    private const float ClonePodScrollBarPadding = 6f;
    private const float HeaderLinkRowHeight = 34f;
    private const float HeaderLinkIconSize = 24f;
    private const float HeaderLinkInnerGap = 6f;
    private const float HeaderLinkGroupsGap = 12f;
    private const float HeaderLinkButtonMinWidth = 90f;
    private const float HeaderLinkButtonExtraWidth = 20f;

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

    #region Sarcophagus

    public float SarcophagusMaxDiagnosisTime = 15f;

    public float SarcophagusMaxPerHediffHealingTime = 30f;

    public float SarcophagusDiagnosisModePowerConsumption = 4000f;

    public float SarcophagusHealingModePowerConsumption = 8000f;

    public float SarcophagusPowerConsumptionReductionFactor = 0.65f;

    public bool SarcophagusApplyAddictionHediff = true;

    public float SarcophagusAddictiveness = 1f;

    public float SarcophagusHighSeverity = 1f;

    public float SarcophagusExistingAddictionSeverityOffset = 0.1f;

    public float SarcophagusNeedLevelOffset = 0.9f;

    #endregion

    public int MaxGateAddresses = 11;

    public int MaxActiveGateQuestSites = 2;

    public bool EnableAsteroidIncidents = true;

    public bool VerboseLogging = false;

    public RimgateLogLevel LogLevel => VerboseLogging ? RimgateLogLevel.Debug : RimgateLogLevel.Warning;

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
        Scribe_Values.Look<int>(ref MaxGateAddresses, "MaxGateAddresses", 11, true);
        Scribe_Values.Look<int>(ref MaxActiveGateQuestSites, "MaxActiveGateQuestSites", 2, true);
        Scribe_Values.Look<bool>(ref EnableAsteroidIncidents, "EnableAsteroidIncidents", true, true);
        Scribe_Values.Look<int>(ref MedicineSkillReq, "MedicineSkillReq", 10, true);
        Scribe_Values.Look<float>(ref StabilizerDeteriorationFactor, "StabilizerDeteriorationRate", 0.5f, true);

        Scribe_Values.Look<float>(ref SarcophagusMaxDiagnosisTime, "SarcophagusMaxDiagnosisTime", 15f, true);
        Scribe_Values.Look<float>(ref SarcophagusMaxPerHediffHealingTime, "SarcophagusMaxPerHediffHealingTime", 30f, true);
        Scribe_Values.Look<float>(ref SarcophagusDiagnosisModePowerConsumption, "SarcophagusDiagnosisModePowerConsumption", 4000f, true);
        Scribe_Values.Look<float>(ref SarcophagusHealingModePowerConsumption, "SarcophagusHealingModePowerConsumption", 8000f, true);
        Scribe_Values.Look<float>(ref SarcophagusPowerConsumptionReductionFactor, "SarcophagusPowerConsumptionReductionFactor", 0.65f, true);
        Scribe_Values.Look<bool>(ref SarcophagusApplyAddictionHediff, "SarcophagusApplyAddictionHediff", true, true);
        Scribe_Values.Look<float>(ref SarcophagusAddictiveness, "SarcophagusAddictiveness", 1f, true);
        Scribe_Values.Look<float>(ref SarcophagusHighSeverity, "SarcophagusHighSeverity", 1f, true);
        Scribe_Values.Look<float>(ref SarcophagusExistingAddictionSeverityOffset, "SarcophagusExistingAddictionSeverityOffset", 0.1f, true);
        Scribe_Values.Look<float>(ref SarcophagusNeedLevelOffset, "SarcophagusNeedLevelOffset", 0.9f, true);

        Scribe_Values.Look<bool>(ref VerboseLogging, "VerboseLogging", false, true);

        ApplyBuffers();
    }

    private void ApplyBuffers()
    {
        _baseCalibrationTicksBuffer = BaseCalibrationTicks.ToString();
        _baseIncubationTicksBuffer = BaseIncubationTicks.ToString();
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Rect headerLinksRect = new(inRect.x, inRect.y, inRect.width, HeaderLinkRowHeight);
        DrawHeaderLinks(headerLinksRect);

        Rect tabsRect = new(
            inRect.x,
            headerLinksRect.yMax + HeaderLinkRowHeight,
            inRect.width,
            TabDrawer.TabHeight);
        DrawTabs(tabsRect);

        Rect contentRect = new(
            inRect.x,
            tabsRect.yMax - TabDrawer.TabHeight, // slight overlap to hide tab content gap
            inRect.width,
            Mathf.Max(0f, inRect.yMax - tabsRect.yMax));
        Widgets.DrawMenuSection(contentRect);

        Rect innerContentRect = contentRect.ContractedBy(ContentPadding);
        switch (_activeTab)
        {
            case SettingsTab.General:
                DrawGeneralTab(innerContentRect);
                break;
            case SettingsTab.Gate:
                DrawGateTab(innerContentRect);
                break;
            case SettingsTab.ClonePod:
                DrawClonePodTab(innerContentRect);
                break;
            case SettingsTab.Sarcophagus:
                DrawSarcophagusTab(innerContentRect);
                break;
            default:
                DrawGeneralTab(innerContentRect);
                break;
        }

        ClampSettingsValues();
    }

    private void DrawHeaderLinks(Rect rowRect)
    {
        float halfWidth = (rowRect.width - HeaderLinkGroupsGap) * 0.5f;
        Rect leftGroupRect = new(rowRect.x, rowRect.y, halfWidth, rowRect.height);
        Rect rightGroupRect = new(leftGroupRect.xMax + HeaderLinkGroupsGap, rowRect.y, halfWidth, rowRect.height);

        if (DrawLeftIconLinkButton(
            leftGroupRect,
            "RG_Settings_Github_Button".Translate(),
            RimgateTex.GithubIconTex))
        {
            Application.OpenURL("https://github.com/mrdav30/Rimgate/wiki");
        }

        if (DrawRightIconLinkButton(
            rightGroupRect,
            "RG_Settings_Discord_Button".Translate(),
            RimgateTex.DiscordIconTex))
        {
            Application.OpenURL("https://discord.gg/mhwK2QFNBA");
        }
    }

    private bool DrawLeftIconLinkButton(Rect groupRect, string label, Texture2D icon)
    {
        float buttonWidth = CalculateHeaderLinkButtonWidth(label, groupRect.width);
        Rect iconRect = new(
            groupRect.x,
            groupRect.y + ((groupRect.height - HeaderLinkIconSize) * 0.5f),
            HeaderLinkIconSize,
            HeaderLinkIconSize);
        Rect buttonRect = new(iconRect.xMax + HeaderLinkInnerGap, groupRect.y, buttonWidth, groupRect.height);

        Widgets.DrawTextureFitted(iconRect, icon, 1f);
        return Widgets.ButtonText(buttonRect, label);
    }

    private bool DrawRightIconLinkButton(Rect groupRect, string label, Texture2D icon)
    {
        float buttonWidth = CalculateHeaderLinkButtonWidth(label, groupRect.width);
        Rect iconRect = new(
            groupRect.xMax - HeaderLinkIconSize,
            groupRect.y + ((groupRect.height - HeaderLinkIconSize) * 0.5f),
            HeaderLinkIconSize,
            HeaderLinkIconSize);
        Rect buttonRect = new(
            iconRect.x - HeaderLinkInnerGap - buttonWidth,
            groupRect.y,
            buttonWidth,
            groupRect.height);

        Widgets.DrawTextureFitted(iconRect, icon, 1f);
        return Widgets.ButtonText(buttonRect, label);
    }

    private static float CalculateHeaderLinkButtonWidth(string label, float groupWidth)
    {
        float maxWidth = Mathf.Max(
            1f,
            groupWidth - HeaderLinkIconSize - HeaderLinkInnerGap);
        float desiredWidth = Mathf.Max(
            HeaderLinkButtonMinWidth,
            label.GetWidthCached() + HeaderLinkButtonExtraWidth);
        return Mathf.Min(maxWidth, desiredWidth);
    }

    private void DrawTabs(Rect tabsRect)
    {
        _tabs.Clear();
        _tabs.Add(new TabRecord(
            "RG_Settings_Tab_General".Translate(),
            delegate { _activeTab = SettingsTab.General; },
            _activeTab == SettingsTab.General));
        _tabs.Add(new TabRecord(
            "RG_Settings_Tab_Gate".Translate(),
            delegate { _activeTab = SettingsTab.Gate; },
            _activeTab == SettingsTab.Gate));
        _tabs.Add(new TabRecord(
            "RG_Settings_Tab_ClonePod".Translate(),
            delegate { _activeTab = SettingsTab.ClonePod; },
            _activeTab == SettingsTab.ClonePod));
        _tabs.Add(new TabRecord(
            "RG_Settings_Tab_Sarcophagus".Translate(),
            delegate { _activeTab = SettingsTab.Sarcophagus; },
            _activeTab == SettingsTab.Sarcophagus));

        TabDrawer.DrawTabs(tabsRect, _tabs);
    }

    private void DrawGeneralTab(Rect contentRect)
    {
        Listing_Standard listing = new();
        listing.Begin(contentRect);

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_General_Label".Translate(), addTopGap: false);
        DrawUtil.DrawCheckbox(listing, "RG_Settings_EnableAsteroidIncidents_Label".Translate(), ref EnableAsteroidIncidents);

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_Logging_Label".Translate());
        DrawUtil.DrawCheckbox(listing, "RG_Settings_VerboseLogging_Label".Translate(), ref VerboseLogging);

        Rect defaultsButtonRect = listing.GetRect(DrawUtil.SliderHeight + 2f);
        if (Widgets.ButtonText(defaultsButtonRect, "RG_Settings_RestoreDefaults_Button".Translate()))
            RestoreDefaults();

        listing.Gap(DrawUtil.SliderGap);
        listing.End();
    }

    private void DrawGateTab(Rect contentRect)
    {
        Listing_Standard listing = new();
        listing.Begin(contentRect);

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_Gate_Label".Translate(), addTopGap: false);
        DrawUtil.DrawIntSlider(
            listing,
            "RG_Settings_MaxGateAddresses_Label".Translate(),
            ref MaxGateAddresses,
            2,
            50);
        DrawUtil.DrawIntSlider(
            listing,
            "RG_Settings_MaxActiveQuestSites_Label".Translate(),
            ref MaxActiveGateQuestSites,
            0,
            10);

        listing.Gap(DrawUtil.SliderGap);
        listing.End();
    }

    private void DrawClonePodTab(Rect contentRect)
    {
        float viewWidth = Mathf.Max(1f, contentRect.width - GenUI.ScrollBarWidth - ClonePodScrollBarPadding);
        float viewHeight = Mathf.Max(contentRect.height, CalculateClonePodViewHeight(viewWidth));
        Rect viewRect = new(0f, 0f, viewWidth, viewHeight);

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

    private void DrawSarcophagusTab(Rect contentRect)
    {
        Listing_Standard listing = new();
        listing.Begin(contentRect);

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_SarcophagusTimings_Label".Translate(), addTopGap: false);
        DrawUtil.DrawFloatSlider(
            listing,
            "RG_Settings_SarcophagusMaxDiagnosisTime_Label".Translate(),
            ref SarcophagusMaxDiagnosisTime,
            1f,
            30f,
            0.5f,
            "0.0",
            " s");
        DrawUtil.DrawFloatSlider(
            listing,
            "RG_Settings_SarcophagusMaxPerHediffHealingTime_Label".Translate(),
            ref SarcophagusMaxPerHediffHealingTime,
            1f,
            30f,
            0.5f,
            "0.0",
            " s");

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_SarcophagusPower_Label".Translate());
        DrawUtil.DrawFloatSlider(
            listing,
            "RG_Settings_SarcophagusDiagnosisModePowerConsumption_Label".Translate(),
            ref SarcophagusDiagnosisModePowerConsumption,
            500f,
            30000f,
            100f,
            "0",
            " W");
        DrawUtil.DrawFloatSlider(
            listing,
            "RG_Settings_SarcophagusHealingModePowerConsumption_Label".Translate(),
            ref SarcophagusHealingModePowerConsumption,
            500f,
            30000f,
            100f,
            "0",
            " W");
        DrawUtil.DrawFloatSlider(
            listing,
            "RG_Settings_SarcophagusPowerConsumptionReductionFactor_Label".Translate(),
            ref SarcophagusPowerConsumptionReductionFactor,
            0f,
            1f,
            0.01f,
            "0.00",
            "x");

        DrawUtil.DrawSectionHeader(listing, "RG_Settings_Section_SarcophagusAfterEffects_Label".Translate());
        DrawUtil.DrawCheckbox(
            listing,
            "RG_Settings_SarcophagusApplyAddictionHediff_Label".Translate(),
            ref SarcophagusApplyAddictionHediff);
        if (SarcophagusApplyAddictionHediff)
        {
            DrawUtil.DrawPercentSlider(
                listing,
                "RG_Settings_SarcophagusAddictiveness_Label".Translate(),
                ref SarcophagusAddictiveness,
                0f,
                1f,
                0.01f);
            DrawUtil.DrawFloatSlider(
                listing,
                "RG_Settings_SarcophagusExistingAddictionSeverityOffset_Label".Translate(),
                ref SarcophagusExistingAddictionSeverityOffset,
                0f,
                2f,
                0.01f,
                "0.00");
            DrawUtil.DrawFloatSlider(
                listing,
                "RG_Settings_SarcophagusNeedLevelOffset_Label".Translate(),
                ref SarcophagusNeedLevelOffset,
                0f,
                2f,
                0.01f,
                "0.00");
            DrawUtil.DrawFloatSlider(
                listing,
                "RG_Settings_SarcophagusHighSeverity_Label".Translate(),
                ref SarcophagusHighSeverity,
                -1f,
                3f,
                0.05f,
                "0.00");
        }

        listing.Gap(DrawUtil.SliderGap);
        listing.End();
    }

    private float CalculateClonePodViewHeight(float availableWidth)
    {
        float sliderHeight = DrawUtil.GetSliderLineHeight();
        float sliderWithInputHeight = DrawUtil.GetSliderWithInputHeight();
        float height = 0f;

        height += DrawUtil.GetSectionHeaderHeight("RG_Settings_Section_CloneTimings_Label".Translate(), availableWidth, addTopGap: false);
        height += 2f * sliderWithInputHeight;

        height += DrawUtil.GetSectionHeaderHeight("RG_Settings_Section_CloneModifiers_Label".Translate(), availableWidth);
        height += 6f * sliderHeight;

        height += DrawUtil.GetSectionHeaderHeight("RG_Settings_Section_CloneBehavior_Label".Translate(), availableWidth);
        height += DrawUtil.GetCheckboxHeight("RG_Settings_EnableCloneIncidents_Label".Translate(), availableWidth);
        height += DrawUtil.GetCheckboxHeight("RG_Settings_CloneTattoos_Label".Translate(), availableWidth);
        height += DrawUtil.GetCheckboxHeight("RG_Settings_NoSkillLoss_Label".Translate(), availableWidth);
        height += DrawUtil.GetCheckboxHeight("RG_Settings_GenerateSocialRelations_Label".Translate(), availableWidth);

        height += DrawUtil.GetSectionHeaderHeight("RG_Settings_Section_CloneFailures_Label".Translate(), availableWidth);
        height += DrawUtil.GetCheckboxHeight("RG_Settings_MinorFailures_Label".Translate(), availableWidth);
        if (MinorFailures)
            height += sliderHeight;

        height += DrawUtil.GetCheckboxHeight("RG_Settings_MajorFailures_Label".Translate(), availableWidth);
        if (MajorFailures)
            height += sliderHeight;

        height += DrawUtil.GetSectionHeaderHeight("RG_Settings_Section_CloneRequirements_Label".Translate(), availableWidth);
        height += 2f * sliderHeight;

        return height + ClonePodBottomPadding;
    }

    private void ClampSettingsValues()
    {
        MaxGateAddresses = Mathf.Clamp(MaxGateAddresses, 2, 50);
        MaxActiveGateQuestSites = Mathf.Clamp(MaxActiveGateQuestSites, 0, 10);
        MinorFailureChance = Mathf.Clamp(MinorFailureChance, 0, 100);
        MajorFailureChance = Mathf.Clamp(MajorFailureChance, 0, 100);
        MedicineSkillReq = Mathf.Clamp(MedicineSkillReq, 0, 20);
        StabilizerBiomassCostReduction = Mathf.Clamp(StabilizerBiomassCostReduction, 0f, 0.95f);
        InducerCalibrationSpeedFactor = Mathf.Max(0.1f, InducerCalibrationSpeedFactor);
        InducerIncubationSpeedFactor = Mathf.Max(0.1f, InducerIncubationSpeedFactor);

        SarcophagusMaxDiagnosisTime = Mathf.Clamp(SarcophagusMaxDiagnosisTime, 1f, 30f);
        SarcophagusMaxPerHediffHealingTime = Mathf.Clamp(SarcophagusMaxPerHediffHealingTime, 1f, 30f);
        SarcophagusDiagnosisModePowerConsumption = Mathf.Clamp(SarcophagusDiagnosisModePowerConsumption, 500f, 30000f);
        SarcophagusHealingModePowerConsumption = Mathf.Clamp(SarcophagusHealingModePowerConsumption, 500f, 30000f);
        SarcophagusPowerConsumptionReductionFactor = Mathf.Clamp01(SarcophagusPowerConsumptionReductionFactor);
        SarcophagusAddictiveness = Mathf.Clamp01(SarcophagusAddictiveness);
        SarcophagusExistingAddictionSeverityOffset = Mathf.Clamp(SarcophagusExistingAddictionSeverityOffset, 0f, 2f);
        SarcophagusNeedLevelOffset = Mathf.Clamp(SarcophagusNeedLevelOffset, 0f, 2f);
        SarcophagusHighSeverity = Mathf.Clamp(SarcophagusHighSeverity, -1f, 3f);
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
        MaxGateAddresses = defaults.MaxGateAddresses;
        MaxActiveGateQuestSites = defaults.MaxActiveGateQuestSites;

        SarcophagusMaxDiagnosisTime = defaults.SarcophagusMaxDiagnosisTime;
        SarcophagusMaxPerHediffHealingTime = defaults.SarcophagusMaxPerHediffHealingTime;
        SarcophagusDiagnosisModePowerConsumption = defaults.SarcophagusDiagnosisModePowerConsumption;
        SarcophagusHealingModePowerConsumption = defaults.SarcophagusHealingModePowerConsumption;
        SarcophagusPowerConsumptionReductionFactor = defaults.SarcophagusPowerConsumptionReductionFactor;
        SarcophagusApplyAddictionHediff = defaults.SarcophagusApplyAddictionHediff;
        SarcophagusAddictiveness = defaults.SarcophagusAddictiveness;
        SarcophagusHighSeverity = defaults.SarcophagusHighSeverity;
        SarcophagusExistingAddictionSeverityOffset = defaults.SarcophagusExistingAddictionSeverityOffset;
        SarcophagusNeedLevelOffset = defaults.SarcophagusNeedLevelOffset;

        EnableAsteroidIncidents = defaults.EnableAsteroidIncidents;
        VerboseLogging = defaults.VerboseLogging;
    }
}
